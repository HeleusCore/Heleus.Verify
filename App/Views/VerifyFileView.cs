using System;
using Heleus.Apps.Shared;
using Heleus.VerifyService;

namespace Heleus.Apps.Verify.Views
{
    public class VerifyFileView : RowView
    {
        readonly ExtLabel _filename;
        readonly ExtLabel _hash;
        readonly ExtLabel _hashtype;
        readonly ExtLabel _size;
        readonly ExtLabel _link;

        public VerifyFileView(bool showLink) : base("VerifyFileView")
        {
            (_, _filename) = AddRow("Filename", "-");
            (_, _hashtype) = AddRow("HashType", "-");
            (_, _hash) = AddRow("Hash", "-");
            _hash.FontStyle = Theme.MicroFont;

            if(showLink)
            {
                (_, _size) = AddRow("Size", "-");
                (_, _link) = AddLastRow("Link", "-");
            }
            else
            {
                (_, _size) = AddLastRow("Size", "-");
            }
        }

        public void Reset()
        {
            _filename.Text = "-";
            _hashtype.Text = "-";
            _hash.Text = "-";
            _size.Text = "-";

            if (_link != null)
                _link.Text = "-";
        }

        public void Update(VerifyFileJson verifyItem)
        {
            if(verifyItem == null)
            {
                Reset();
                return;
            }

            _filename.Text = verifyItem.name;
            _hash.Text = verifyItem.hash;
            _hashtype.Text = Tr.Get($"HashTypes.{verifyItem.hashtype}");
            _size.Text = Tr.Get("VerifyFileView.SizeInfo", verifyItem.length);

            if (_link != null)
            {
                if (string.IsNullOrEmpty(verifyItem.link))
                    _link.Text = "-";
                else
                    _link.Text = verifyItem.link;
            }
        }
    }
}
