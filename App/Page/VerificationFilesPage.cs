using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.Verify.Views;
using Heleus.Cryptography;
using Heleus.VerifyService;

namespace Heleus.Apps.Verify
{
    public class VerificationFilesPage : StackPage
    {
        enum VerifyFileAction
        {
            Cancel,
            Verify,
            OpenLink
        }

        async Task<VerifyFileAction> DisplayVerifyFileAction()
        {
            var cancel = Tr.Get("Common.Cancel");

            var verify = T("Verify");
            var openlink = T("OpenLink");

            var result = await DisplayActionSheet(Tr.Get("Common.Action"), cancel, null, verify, openlink);
            if (result == verify)
                return VerifyFileAction.Verify;
            if (result == openlink)
                return VerifyFileAction.OpenLink;
            return VerifyFileAction.Cancel;
        }

        async Task Verify(ButtonViewRow<VerifyFileView> button)
        {
            if (!(button.Tag is VerifyFileJson verify))
                return;

            if (string.IsNullOrEmpty(verify.link))
            {
                await Verify(verify);
            }
            else
            {
                var result = await DisplayVerifyFileAction();
                if (result == VerifyFileAction.Verify)
                {
                    await Verify(verify);
                }
                else if (result == VerifyFileAction.OpenLink)
                {
                    if (!string.IsNullOrEmpty(verify.link))
                        UIApp.OpenUrl(new Uri(verify.link));
                }
            }
        }

        async Task Verify(VerifyFileJson verifyFile)
        {
            using (var file = await UIApp.OpenFilePicker2())
            {
                if (file.Valid)
                {
                    IsBusy = true;
                    Toast(".VerificationFilePage.Wait");

                    var valid = await Task.Run(() => VerifyFileJson.CheckHash(file.Stream, verifyFile.GetHash(), verifyFile.length));
                    IsBusy = false;
                    await MessageAsync(valid ? ".VerifyPage.Success" : ".VerifyPage.Failure");
                }
            }
        }

        public VerificationFilesPage(VerifyJson verify) : base("VerificationFilesPage")
        {
            AddTitleRow("Title");

            AddHeaderRow();

            foreach (var file in verify.files)
            {
                var view = new VerifyFileView(true);
                view.Update(file);

                var row = AddButtonViewRow(view, Verify);
                row.Tag = file;
            }

            AddFooterRow();
        }
    }
}
