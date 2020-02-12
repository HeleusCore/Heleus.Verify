using System;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Transactions;
using Heleus.VerifyService;

namespace Heleus.Apps.Verify.Views
{
    public class VerifyView : RowView
    {
        readonly ExtLabel _description;
        readonly ExtLabel _link;
        readonly ExtLabel _date;
        readonly ExtLabel _account;
        readonly ExtLabel _id;
        readonly ExtLabel _verifyLink;

        public VerifyView() : base("VerifyView")
        {
            (_, _description) = AddRow("Description", "-");
            (_, _link) = AddRow("Link", "-");
            (_, _date) = AddRow("Date", "-");
            (_, _id) = AddRow("VerifyId", "-");
            (_, _verifyLink) = AddRow("VerifyLink", "-");
            (_, _account) = AddLastRow("Account", "-");
        }

        public void Reset()
        {
            _description.Text = "-";
            _link.Text = "-";
            _date.Text = "-";
            _account.Text = "-";
            _id.Text = "-";
            _verifyLink.Text = "-";
        }

        public void Update(VerificationResult verifyResult)
        {
            var transaction = verifyResult?.Transaction;
            var verify = verifyResult?.Verify;
            var node = verifyResult?.ServiceNode;

            if (transaction != null && verify != null)
            {
                _description.Text = verify.description;
                _link.Text = string.IsNullOrWhiteSpace(verify.link) ? "-" : verify.link;
                _date.Text = Time.DateTimeString(transaction.Timestamp);
                _account.Text = transaction.AccountId.ToString();
                _id.Text = transaction.TransactionId.ToString();
                _verifyLink.Text = VerifyApp.Current.GetRequestCode(node, transaction.ChainIndex, ViewVerificationSchemeAction.ActionName, transaction.TransactionId);
            }
            else
            {
                Reset();
            }
        }
    }
}
