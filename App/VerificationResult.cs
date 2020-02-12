using Heleus.Base;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.VerifyService;

namespace Heleus.Apps.Shared
{
    public class VerificationResult
    {
        public readonly AttachementDataTransaction Transaction;
        public readonly VerifyJson Verify;
        public readonly ServiceNode ServiceNode;

        public VerificationResult(AttachementDataTransaction transaction, VerifyJson verify, ServiceNode serviceNode)
        {
            Transaction = transaction;
            Verify = verify;
            ServiceNode = serviceNode;
        }
    }
}
