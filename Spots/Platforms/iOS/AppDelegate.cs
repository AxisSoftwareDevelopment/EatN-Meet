using eatMeet;
using eatMeet.Database;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using UserNotifications;
using UIKit;

namespace Spots;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		// Request notification permission on iOS
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge,
			(granted, error) =>
			{
				if (granted)
				{
					// Register for remote notifications to receive device token
					MainThread.BeginInvokeOnMainThread(() =>
					{
						UIApplication.SharedApplication.RegisterForRemoteNotifications();
						// Update token in Firestore (uses CloudMessagingManager internally)
						Task.Run(async () => await DatabaseManager.UpdateCurrentUserFCMToken());
					});
				}
			});

		return base.FinishedLaunching(application, launchOptions);
	}
}
