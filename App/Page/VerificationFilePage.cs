using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.Verify.Views;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.VerifyService;

namespace Heleus.Apps.Verify
{
    public class VerificationFilePage : StackPage
    {
        readonly AddVerificationPage _verificationPage;
        readonly VerifyFileJson _item;

        readonly EntryRow _link;

        readonly VerifyFileView _verifyView;
        string _fileName;
        Hash _fileHash;
        long _length;

        async Task HashFile(OpenFile file)
        {
            IsBusy = true;
            Toast("Wait");

            _fileHash = null;
            _fileName = null;
            _length = 0;

            await Task.Run(() =>
            {
                try
                {
                    (var hash, var length) = VerifyFileJson.GenerateHash(file.Stream);
                    if (hash != null)
                    {
                        _fileName = file.Name;
                        _fileHash = hash;
                        _length = length;
                    }
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                }
            });

            if (_fileHash != null)
                _verifyView.Update(new VerifyFileJson { name = _fileName, hashtype = _fileHash.HashType.ToString().ToLower(), hash = Hex.ToString(_fileHash.RawData), length = _length, link = _link.Edit.Text });

            IsBusy = false;
        }

        async Task Select(ButtonRow button)
        {
            using (var file = await UIApp.OpenFilePicker2(null))
            {
                if (file.Valid)
                {
                    await HashFile(file);
                }
            }

            Status.ReValidate();
        }

        async Task Submit(ButtonRow button)
        {
            _item.name = _fileName;
            _item.hash = Hex.ToString(_fileHash.RawData);
            _item.hashtype = _fileHash.HashType.ToString().ToLower();
            _item.length = _length;
            _item.link = _link.Edit.Text;

            _verificationPage.AddUpdateFile(_item);

            await Navigation.PopAsync();
        }

        public static async Task Open(AddVerificationPage verificationPage, VerifyFileJson verifyItem)
        {
            if(verifyItem != null)
            {
                await verificationPage.Navigation.PushAsync(new VerificationFilePage(verificationPage, verifyItem));
            }
            else
            {
                var file = await UIApp.OpenFilePicker2(null);
                if(file.Valid)
                {
                    await verificationPage.Navigation.PushAsync(new VerificationFilePage(verificationPage, null, file));
                }
            }
        }

        VerificationFilePage(AddVerificationPage verificationPage, VerifyFileJson verifyItem, OpenFile file = null) : base("VerificationFilePage")
        {
            _verificationPage = verificationPage;
            _item = verifyItem ?? new VerifyFileJson();

            AddTitleRow("Title");
            AddHeaderRow("File");
            Status.AddBusyView(AddButtonRow("Select", Select));

            _link = AddEntryRow(null, "Link");
            _link.SetDetailViewIcon(Icons.RowLink);

            _verifyView = new VerifyFileView(false);
            AddViewRow(_verifyView);

            if (verifyItem != null)
            {
                _link.Edit.Text = verifyItem.link;
                _fileName = verifyItem.name;
                _fileHash = verifyItem.GetHash();
                _length = verifyItem.length;
                _verifyView.Update(verifyItem);
            }

            Status.Add(_link.Edit, T("LinkStatus"), (view, entry, newText, oldText) =>
            {
                if (string.IsNullOrEmpty(newText))
                    return true;

                return newText.IsValdiUrl(true);
            });

            Status.Add(T("FileStatus"), (sv) =>
            {
                return !string.IsNullOrEmpty(_fileName) && _fileHash != null && _length > 0;
            });

            AddFooterRow();

            AddSubmitRow("Submit", Submit);

            if(file != null)
                UIApp.Run(() => HashFile(file));
        }
    }
}
