using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Heleus.Apps.Shared.Android
{
	[Activity(Label = "Heleus Verify", Icon = "@mipmap/icon", RoundIcon = "@mipmap/icon_round", Theme = "@style/SplashTheme", MainLauncher = true, AlwaysRetainTaskState = true,
		LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.KeyboardHidden)]
	[IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, 
                  DataScheme = "heleusverify")]
	[IntentFilter(new string[] { Intent.ActionView },Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
				  DataHost = "heleuscore.com", AutoVerify = true, DataScheme = "http", DataPathPrefix = "/verify/request/")]
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
                  DataHost = "heleuscore.com", AutoVerify = true, DataScheme = "https", DataPathPrefix = "/verify/request/")]

	public partial class MainActivity
    {
        void InitFirebase()
        {
            //Firebase.FirebaseApp.InitializeApp(this);
            //App.Current.EnableRemoteNotifications();
        }
		void HandleActivityResult(int requestCode, Result resultCode, Intent data)
        {
            
        }
	}
}
