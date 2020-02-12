using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;

namespace Heleus.Apps.Verify
{
    public class ViewVerificationSchemeAction : ServiceNodeSchemeAction
    {
        public const string ActionName = "verification";

        public readonly long VerificationId;
        public override bool IsValid => VerificationId > 0;


        public ViewVerificationSchemeAction(SchemeData schemeData) : base(schemeData)
        {
            GetLong(StartIndex, out VerificationId);
        }

        public override async Task Run()
        {
            if (!IsValid)
                return;

            var serviceNode = await GetServiceNode();
            if (serviceNode == null)
                return;

            var app = UIApp.Current;
            if (app?.CurrentPage != null)
            {
                if (app.MainTabbedPage != null)
                {
                    app.MainTabbedPage.ShowPage(typeof(VerifyPage));
                    await app.CurrentPage.Navigation.PopToRootAsync();
                }
                else if (app.MainMasterDetailPage != null)
                {
                    await app.MainMasterDetailPage.MenuPage.ShowPage(typeof(VerifyPage));
                }

                var page = (app.CurrentPage as VerifyPage);
                if(page != null)
                    await page.UpdateFromScheme(serviceNode, VerificationId);
            }
        }
    }
}
