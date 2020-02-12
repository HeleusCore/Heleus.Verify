using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.Verify.Views;
using Heleus.Transactions;
using Heleus.VerifyService;

namespace Heleus.Apps.Verify
{
    public class AddVerificationPage : StackPage
    {
        VerifyJson _verify;

        EntryRow _description;
        EntryRow _link;
        ButtonRow _addButton;
        SubmitAccountButtonRow _submitAccount;

        Task Submit(ButtonRow button)
        {
            var files = GetVerifyFiles();
            var description = _description.Edit.Text;
            var link = _link.Edit.Text;

            _verify = new VerifyJson { description = description, link = link, files = files };
            IsBusy = true;
            UIApp.Run(() => VerifyApp.Current.UploadVerification(_submitAccount.SubmitAccount, _verify));

            return Task.CompletedTask;
        }

        async Task AddFile(ButtonRow button)
        {
            await VerificationFilePage.Open(this, null);
        }

        List<VerifyFileJson> GetVerifyFiles()
        {
            var result = new List<VerifyFileJson>();
            var buttons = GetHeaderSectionRows("Files");
            foreach (var button in buttons)
            {
                if (button.Tag is VerifyFileJson verifyFile)
                {
                    result.Add(verifyFile);
                }
            }

            return result;
        }

        void Clear()
        {
            _description.Edit.Text = string.Empty;
            _link.Edit.Text = string.Empty;
            ClearHeaderSection("Files", (row) => row.Tag is VerifyFileJson);
        }

        async Task EditFile(ButtonViewRow<VerifyFileView> button)
        {
            var cancel = Tr.Get("Common.Cancel");
            var moveUp = Tr.Get("Common.MoveUp");
            var moveDown = Tr.Get("Common.MoveDown");
            var edit = Tr.Get("Common.Edit");
            var delete = Tr.Get("Common.Delete");
            var openlink = T("OpenLink");

            var result = await DisplayActionSheet(Tr.Get("Common.Action"), cancel, null, moveUp, moveDown, edit, delete, openlink);

            if (result == edit)
            {
                await VerificationFilePage.Open(this, button.Tag as VerifyFileJson);
            }
            else if (result == delete)
            {
                Status.RemoveBusyView(button);
                RemoveView(button);
                Status.ReValidate();
            }
            else if (result == moveUp || result == moveDown)
            {
                var idx = StackLayout.Children.IndexOf(button);
                if (idx > 0)
                {
                    var hasNext = (StackLayout.Children[idx + 1] as StackRow)?.Tag is VerifyFileJson;
                    var hasPrev = (StackLayout.Children[idx - 1] as StackRow)?.Tag is VerifyFileJson;

                    if (hasNext && result == moveDown)
                    {
                        StackLayout.Children.RemoveAt(idx);
                        StackLayout.Children.Insert(idx + 1, button);
                    }
                    else if (hasPrev && result == moveUp)
                    {
                        StackLayout.Children.RemoveAt(idx);
                        StackLayout.Children.Insert(idx - 1, button);
                    }
                }
            }
            else if (result == openlink)
            {
                if (button.Tag is VerifyFileJson verify && !string.IsNullOrEmpty(verify.link))
                    UIApp.OpenUrl(new Uri(verify.link));
            }
        }

        public void AddUpdateFile(VerifyFileJson verifyItem)
        {
            var buttons = GetHeaderSectionRows("Files");
            foreach (var button in buttons)
            {
                if (button.Tag == verifyItem)
                {
                    var v = (button as ButtonRow).RowLayout.Children.Last() as VerifyFileView;
                    v.Update(verifyItem);
                    Status.ReValidate();
                    return;
                }
            }

            AddIndex = _addButton;
            AddIndexBefore = true;

            var view = new VerifyFileView(true);
            view.Update(verifyItem);

            var newButton = AddButtonViewRow(view, EditFile);
            newButton.Tag = verifyItem;
            Status.AddBusyView(newButton);
            Status.ReValidate();
        }

        async Task VerifyUploaded(VerifyUploadEvent uploadEvent)
        {
            IsBusy = false;

            if (uploadEvent.Response.TransactionResult == TransactionResultTypes.Ok)
            {
                var transaction = uploadEvent.Response.Transaction as Transaction;
                var link = VerifyApp.Current.GetRequestCode(uploadEvent.ServiceNode, transaction.ChainIndex, ViewVerificationSchemeAction.ActionName, transaction.TransactionId);
                UIApp.CopyToClipboard(link);

                try
                {
                    if (await ConfirmTextAsync(T("Success", link)))
                        await Navigation.PushAsync(new VerifyPage(new VerificationResult(uploadEvent.Response.Transaction as AttachementDataTransaction, _verify, uploadEvent.ServiceNode)));
                }
                catch { }

                await ScrollToTop();
                Clear();
            }
            else
            {
                await ErrorTextAsync(uploadEvent.Response.GetErrorMessage());
            }
        }

        async Task SelectSubmitAccount(SubmitAccountButtonRow<SubmitAccount> button)
        {
            await Navigation.PushAsync(new SubmitAccountsPage(ServiceNodeManager.Current.GetSubmitAccounts<SubmitAccount>(), (submitAccount) =>
            {
                _submitAccount.SubmitAccount = submitAccount;
                VerifyApp.Current.SetLastUsedSubmitAccount(submitAccount);
            }));
        }

        public AddVerificationPage() : base("AddVerificationPage")
        {
            Subscribe<VerifyUploadEvent>(VerifyUploaded);
            Subscribe<ServiceAccountAuthorizedEvent>(AccountAuth);
            Subscribe<ServiceAccountImportEvent>(AccountImport);
            Subscribe<ServiceNodesLoadedEvent>(Loaded);

            SetupPage();
        }

        Task Loaded(ServiceNodesLoadedEvent arg)
        {
            if (_submitAccount != null && _submitAccount.SubmitAccount == null)
                _submitAccount.SubmitAccount = VerifyApp.Current.GetLastUsedSubmitAccount<SubmitAccount>();
            return Task.CompletedTask;
        }

        Task AccountImport(ServiceAccountImportEvent arg)
        {
            SetupPage();
            return Task.CompletedTask;
        }

        Task AccountAuth(ServiceAccountAuthorizedEvent arg)
        {
            SetupPage();
            return Task.CompletedTask;
        }

        void SetupPage()
        {
            StackLayout.Children.Clear();

            if (!ServiceNodeManager.Current.HadUnlockedServiceNode)
            {
                AddTitleRow("VerifyPage.Title");

                AddInfoRow("VerifyInfo");

                ServiceNodesPage.AddAuthorizeSection(ServiceNodeManager.Current.NewDefaultServiceNode, this, false);
            }
            else
            {
                AddTitleRow("Title");
                AddHeaderRow("Info");

                _description = AddEntryRow(null, "Description");
                _description.SetDetailViewIcon(Icons.Pencil);
                _link = AddEntryRow(null, "Link");
                _link.SetDetailViewIcon(Icons.RowLink);

                Status.Add(_description.Edit, T("DescriptionStatus"), (view, entry, newText, oldTex) =>
                {
                    if (string.IsNullOrEmpty(newText))
                        return false;

                    return true;
                }).
                Add(_link.Edit, T("LinkStatus"), (view, entry, newText, oldText) =>
                {
                    if (string.IsNullOrEmpty(newText))
                        return true;

                    return newText.IsValdiUrl(true);
                });

                AddFooterRow();

                AddHeaderRow("Files");

                _addButton = AddButtonRow("AddFile", AddFile);

                Status.Add(T("FilesStatus"), (sv) =>
                {
                    return GetVerifyFiles().Count > 0;
                });

                AddFooterRow();

                AddSubmitRow("Submit", Submit);

                AddHeaderRow("Common.SubmitAccount");
                _submitAccount = AddRow(new SubmitAccountButtonRow(VerifyApp.Current.GetLastUsedSubmitAccount<SubmitAccount>(), this, SelectSubmitAccount));
                AddInfoRow("Common.SubmitAccountInfo");
                AddFooterRow();
            }
        }
    }
}
