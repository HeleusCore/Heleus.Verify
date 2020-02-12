using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Heleus.Apps.Verify;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Network.Client;
using Heleus.Transactions;
using Heleus.Transactions.Features;
using Heleus.VerifyService;
using TinyJson;

namespace Heleus.Apps.Shared
{
    public class VerifyApp : AppBase<VerifyApp>
    {
        protected override async Task ServiceNodesLoaded(ServiceNodesLoadedEvent arg)
        {
            await base.ServiceNodesLoaded(arg);
            await UIApp.Current.SetFinishedLoading();
        }

        public override ServiceNode GetLastUsedServiceNode(string key = "default")
        {
            var node = base.GetLastUsedServiceNode(key);
            if (node != null)
                return node;

            return ServiceNodeManager.Current.FirstServiceNode;
        }

        public override T GetLastUsedSubmitAccount<T>(string key = "default")
        {
            var account = base.GetLastUsedSubmitAccount<T>(key);
            if (account != null)
                return account;

            var node = GetLastUsedServiceNode(key);
            if(node != null)
            {
                return node.GetSubmitAccounts<T>().FirstOrDefault();
            }

            return null;
        }

        public override void UpdateSubmitAccounts()
        {
            var index = VerifyServiceInfo.VerifyIndex;

            foreach(var serviceNode in ServiceNodeManager.Current.ServiceNodes)
            {
                foreach(var serviceAccount in serviceNode.ServiceAccounts.Values)
                {
                    var keyIndex = serviceAccount.KeyIndex;

                    if(!serviceNode.HasSubmitAccount(keyIndex, index))
                    {
                        serviceNode.AddSubmitAccount(new SubmitAccount(serviceNode, keyIndex, index, false));
                    }
                }
            }
        }

        public async Task<HeleusClientResponse> UploadVerification(SubmitAccount submitAccount, VerifyJson verifyJson)
        {
            var serviceNode = submitAccount?.ServiceNode;
            var result = await SetSubmitAccount(submitAccount);
            if (result != null)
                goto end;

            var attachements = serviceNode.Client.NewAttachements(VerifyServiceInfo.ChainIndex);
            if (verifyJson != null)
            {
                var json = verifyJson.ToJson();
                attachements.AddStringAttachement(VerifyServiceInfo.JsonFileName, json);
            }

            result = await serviceNode.Client.UploadAttachements(attachements, (transaction) =>
            {
                transaction.EnableFeature<AccountIndex>(AccountIndex.FeatureId).Index = VerifyServiceInfo.VerifyIndex;

                transaction.PrivacyType = DataTransactionPrivacyType.PublicData;
            });

        end:
            await UIApp.PubSub.PublishAsync(new VerifyUploadEvent(result, serviceNode));

            return result;
        }

        public async Task<VerificationResult> GetVerificationResult(TransactionDownloadData<Transaction> transaction)
        {
            if (transaction == null || !(transaction.Transaction is AttachementDataTransaction attachementTransaction))
                return null;

            var serviceNode = transaction.Tag as ServiceNode;

            var result = await serviceNode.GetTransactionDownloadManager(transaction.Transaction.ChainIndex).DownloadTransactionAttachement(transaction);
            if(result.AttachementsState == TransactionAttachementsState.Ok)
            {
                var attachement = result.GetAttachementData(VerifyServiceInfo.JsonFileName);
                if(attachement != null)
                {
                    var hash = Hash.Generate(Protocol.AttachementsHashType, attachement);
                    if (hash == attachementTransaction.Items[0].DataHash)
                    {
                        var json = Encoding.UTF8.GetString(attachement);
                        var verify = json.FromJson<VerifyJson>();
                        if (verify != null)
                        {
                            return new VerificationResult(attachementTransaction, verify, serviceNode);
                        }
                    }
                }
            }

            return null;
        }

        public async Task<VerificationResult> DownloadVerificationResult(ServiceNode serviceNode, long transactionId)
        {
            try
            {
                if (serviceNode == null)
                    return null;

                var transaction = await serviceNode.GetTransactionDownloadManager(VerifyServiceInfo.ChainIndex).DownloadTransaction(transactionId, true, false);
                if(transaction.Count == 1)
                {
                    transaction.Transactions[0].Tag = serviceNode;
                    return await GetVerificationResult(transaction.Transactions[0]);
                }
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex);
            }

            return null;
        }
    }
}
