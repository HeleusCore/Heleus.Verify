using System;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Transactions;

namespace Heleus.Apps.Verify.Views
{
    public class VerifyTransactionView : RowView
    {
        readonly ExtLabel _id;
        readonly ExtLabel _date;

        public VerifyTransactionView(AttachementDataTransaction transaction) : base("VerifyTransactionView")
        {
            (_, _id) = AddRow("TransactionId", null);
            (_, _date) = AddLastRow("Date", null);

            Update(transaction);
        }

        public void Update(AttachementDataTransaction transaction)
        {
            _id.Text = transaction.TransactionId.ToString();
            _date.Text = Time.DateTimeString(transaction.Timestamp);
        }
    }
}
