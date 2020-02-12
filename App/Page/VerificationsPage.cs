using System;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.Verify.Views;
using Heleus.Network.Client;
using Heleus.Transactions;
using Heleus.VerifyService;

namespace Heleus.Apps.Verify
{
    public class VerificationsPage : StackPage
    {
        ButtonRow _more;

        public VerificationsPage() : base("VerificationsPage")
        {
            Subscribe<TransactionDownloadEvent<Transaction>>(AccountTransactionsDownloaded);
            Subscribe<ServiceAccountUnlockedEvent>(AccountUnlocked);

            SetupPage();
        }

        Task AccountUnlocked(ServiceAccountUnlockedEvent unlockedEvent)
        {
            ClearPage();
            SetupPage();

            return Task.CompletedTask;
        }

        public override void OnOpen()
        {
            base.OnOpen();
            Download();
        }

        void ClearPage()
        {
            StackLayout.Children.Clear();
        }

        void SetupPage()
        {
            if (!ServiceNodeManager.Current.HadUnlockedServiceNode)
            {
                AddTitleRow("VerifyPage.Title");

                AddInfoRow("AddVerificationPage.VerifyInfo");

                ServiceNodesPage.AddAuthorizeSection(ServiceNodeManager.Current.NewDefaultServiceNode, this, false);
            }
            else
            {
                AddTitleRow("Title");

                ToolbarItems.Add(new ExtToolbarItem(Tr.Get("Common.Refresh"), null, () =>
                {
                    Download(false);
                    return Task.CompletedTask;
                }));

                AddHeaderRow("RecentTransactions");
                AddFooterRow();

                Download();
            }
        }

        Task More(ButtonRow button)
        {
            Download(true);
            return Task.CompletedTask;
        }

        void Download(bool queryOlder = false)
        {
            if (!ServiceNodeManager.Current.Ready)
                return;

            IsBusy = true;
            UIApp.Run(() => VerifyApp.Current.DownloadAccountIndexTransactions(VerifyServiceInfo.ChainIndex, VerifyServiceInfo.VerifyIndex, (download) => download.QueryOlder = queryOlder));
        }

        async Task Select(ButtonViewRow<VerifyTransactionView> button)
        {
            IsBusy = true;

            if(button.Tag is TransactionDownloadData<Transaction> transaction)
            {
                var verification = await VerifyApp.Current.GetVerificationResult(transaction);
                if(verification != null)
                {
                    await Navigation.PushAsync(new VerifyPage(verification));
                }
            }

            IsBusy = false;
        }

        Task AccountTransactionsDownloaded(TransactionDownloadEvent<Transaction> downloadedEvent)
        {
            AddIndexBefore = false;
            AddIndex = GetRow("RecentTransactions");
            var rows = GetHeaderSectionRows("RecentTransactions");

            var transactions = downloadedEvent.GetSortedTransactions(TransactionSortMode.TimestampDescening);

            if (transactions.Count > 0)
            {
                for (var i = 0; i < transactions.Count; i++)
                {
                    var transaction = transactions[i];
                    var endpoint = transaction.Tag as ServiceNode;
                    if (i < rows.Count && rows[i] is ButtonViewRow<VerifyTransactionView> button)
                    {
                        button.View.Update(transaction.Transaction as AttachementDataTransaction);
                        button.RowLayout.SetAccentColor(endpoint.AccentColor);
                    }
                    else
                    {
                        button = AddButtonViewRow(new VerifyTransactionView(transaction.Transaction as AttachementDataTransaction), Select);
                        button.RowLayout.SetAccentColor(endpoint.AccentColor);
                    }

                    button.Tag = transaction;
                    AddIndex = button;
                }

                if(downloadedEvent.HasMore)
                {
                    if(_more == null)
                        _more = AddButtonRow("MoreButton", More);
                }
                else
                {
                    RemoveView(_more);
                    _more = null;
                }

            }
            else
            {
                Toast("NoTransactions");
            }

            IsBusy = false;

            return Task.CompletedTask;
        }
    }
}
