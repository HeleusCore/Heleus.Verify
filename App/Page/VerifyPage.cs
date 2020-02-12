using System;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.Verify.Views;
using Heleus.Base;
using Heleus.Transactions;
using Heleus.VerifyService;

namespace Heleus.Apps.Verify
{
    public class VerifyPage : StackPage
    {
        readonly EntryRow _transactionId;
        readonly VerifyView _verifyView;

        readonly ButtonRow _link;
        readonly ButtonRow _verifyLink;
        readonly ButtonRow _copyVerifyLink;
        readonly ButtonRow _account;

        readonly ButtonRow _verifyButton;
        readonly ButtonRow _viewFiles;

        readonly ServiceNodeButtonRow _serviceNode;

        public async Task UpdateFromScheme(ServiceNode serviceNode, long verificationId)
        {
            if(_transactionId != null && _serviceNode != null)
            {
                if(serviceNode != _serviceNode.ServiceNode)
                {
                    if (!await ConfirmTextAsync(Tr.Get("Messages.SwitchServiceNode", serviceNode.Name ?? serviceNode.Endpoint.AbsoluteUri)))
                        return;

                    _serviceNode.ServiceNode = serviceNode;
                    VerifyApp.Current.SetLastUsedServiceNode(serviceNode);
                }

                _transactionId.Edit.Text = verificationId.ToString();
                UIApp.Run(() => Search(null));
            }
        }

        public void Update(VerificationResult verify)
        {
            if (verify != null)
            {
                _verifyView.Update(verify);
                if (!string.IsNullOrWhiteSpace(verify.Verify.link))
                {
                    _link.IsEnabled = true;
                    _link.Tag = verify.Verify.link;
                }
                else
                {
                    _link.IsEnabled = false;
                }
                _verifyLink.IsEnabled = _copyVerifyLink.IsEnabled = true;
                _verifyLink.Tag = _copyVerifyLink.Tag = VerifyApp.Current.GetRequestCode(verify.ServiceNode, verify.Transaction.ChainIndex, ViewVerificationSchemeAction.ActionName, verify.Transaction.TransactionId);
                _account.Tag = verify.Transaction;
                _account.IsEnabled = true;

                _verifyButton.IsEnabled = true;
                _viewFiles.IsEnabled = true;
                _verifyButton.Tag = _viewFiles.Tag = verify.Verify;
            }
            else
            {
                _verifyView.Reset();
                _verifyButton.IsEnabled = false;
                _viewFiles.IsEnabled = false;
                _link.IsEnabled = false;
                _verifyLink.IsEnabled = false;
                _copyVerifyLink.IsEnabled = false;
                _account.IsEnabled = false;

                ClearHeaderSection("Search", (row) =>
                {
                    return row.Tag is VerifyFileJson;
                });
            }
        }

        async Task Search(ButtonRow button)
        {
            if (IsBusy)
                return;
            IsBusy = true;

            VerificationResult verify = null;
            if (_transactionId != null && _serviceNode != null && long.TryParse(_transactionId.Edit.Text, out var transactionId))
            {
                verify = await VerifyApp.Current.DownloadVerificationResult(_serviceNode.ServiceNode, transactionId);

                if (verify == null)
                    await ErrorAsync("ValidationNotFound");
            }

            IsBusy = false;

            Update(verify);
        }

        Task Copy(ButtonRow button)
        {
            if(button.Tag is string url)
            {
                UIApp.CopyToClipboard(url);
                Toast("VerifyCopied");
            }

            return Task.CompletedTask;
        }

        async Task Account(ButtonRow button)
        {
            if (button.Tag is DataTransaction t)
            {
                await Navigation.PushAsync(new ViewProfilePage(t.AccountId));
            }
        }

        async Task VerifyFile(ButtonRow button)
        {
            var verify = button.Tag as VerifyJson;

            using (var file = await UIApp.OpenFilePicker2(null))
            {
                if (file.Valid)
                {
                    IsBusy = true;
                    Toast(".VerificationFilePage.Wait");

                    var valid = await Task.Run(() =>
                    {
                        try
                        {
                            (var hash, var length) = VerifyFileJson.GenerateHash(file.Stream);
                            if (hash != null)
                            {
                                foreach(var f in verify.files)
                                {
                                    var filehash = f.GetHash();
                                    if (hash == filehash && f.length == file.Stream.Length)
                                        return true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.IgnoreException(ex);
                        }

                        return false;
                    });

                    IsBusy = false;
                    await MessageAsync(valid ? "Success" : "Failure");
                }
            }
        }

        async Task ViewFiles(ButtonRow button)
        {
            var verify = button.Tag as VerifyJson;
            await Navigation.PushAsync(new VerificationFilesPage(verify));
        }

        async Task SelectServiceNode(ServiceNodeButtonRow button)
        {
            await Navigation.PushAsync(new ServiceNodesPage((serviceNode) =>
            {
                button.ServiceNode = serviceNode;
                VerifyApp.Current.SetLastUsedServiceNode(serviceNode);
            }));
        }

        public VerifyPage() : this(null)
        {
        }

        public VerifyPage(VerificationResult result) : base("VerifyPage")
        {
            Subscribe<ServiceNodesLoadedEvent>(ServiceNodesLoaded);

            var viewResult = result != null;

            AddTitleRow("Title");

            if (!viewResult)
            {
                AddHeaderRow("Search");

                _transactionId = AddEntryRow(null, "TransactionId");
                _transactionId.SetDetailViewIcon(Icons.ShieldCheck);
                _transactionId.Edit.TextChanged += (sender, e) =>
                {
                    StatusValidators.PositiveNumberValidator(null, _transactionId.Edit, e.NewTextValue, e.OldTextValue);
                };

                AddSubmitRow("SearchButton", Search, false);
                AddInfoRow("SearchInfo");
                AddFooterRow();
            }

            _verifyButton = AddButtonRow("VerifyFile", VerifyFile);
            _verifyButton.RowStyle = Theme.SubmitButton;
            _verifyButton.IsEnabled = false;

            _verifyView = new VerifyView();
            AddViewRow(_verifyView);

            _viewFiles = AddButtonRow("ViewFiles", ViewFiles);
            _viewFiles.IsEnabled = false;

            _link = AddLinkRow("Link", "");
            _link.IsEnabled = false;
            _verifyLink = AddLinkRow("VerifyLink", "");
            _verifyLink.IsEnabled = false;
            _copyVerifyLink = AddButtonRow("CopyVerifyLink", Copy);
            _copyVerifyLink.IsEnabled = false;
            _account = AddButtonRow("Account", Account);
            _account.IsEnabled = false;

            AddFooterRow();

            if(!viewResult)
            {
                AddHeaderRow("Common.ServiceNode");
                _serviceNode = AddRow(new ServiceNodeButtonRow(VerifyApp.Current.GetLastUsedServiceNode(), this, SelectServiceNode));
                AddInfoRow("Common.ServiceNodeInfo");
                AddFooterRow();
            }

            if (viewResult)
                Update(result);
        }

        Task ServiceNodesLoaded(ServiceNodesLoadedEvent arg)
        {
            if (_serviceNode != null && _serviceNode.ServiceNode == null)
                _serviceNode.ServiceNode = VerifyApp.Current.GetLastUsedServiceNode();

            return Task.CompletedTask;
        }
    }
}
