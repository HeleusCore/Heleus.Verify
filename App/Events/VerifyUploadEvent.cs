using Heleus.Apps.Shared;
using Heleus.Network.Client;

namespace Heleus.Apps.Verify
{
    public class VerifyUploadEvent : ServiceNodeEvent
    {
        public readonly HeleusClientResponse Response;

        public VerifyUploadEvent(HeleusClientResponse response, ServiceNode serviceNode) : base(serviceNode)
        {
            Response = response;
        }
    }
}
