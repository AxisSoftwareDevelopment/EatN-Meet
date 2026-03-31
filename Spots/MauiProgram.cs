using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Bundled.Shared;

using eatMeet.Database;
using eatMeet.Models;
using Plugin.Firebase.CloudMessaging;
using eatMeet.Notifications;



#if IOS
using Plugin.Firebase.Bundled.Platforms.iOS;
#else
using Plugin.Firebase.Bundled.Platforms.Android;
#endif

namespace eatMeet;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiMaps()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.RegisterFirebaseServices();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events => {
#if IOS
            events.AddiOS(iOS => iOS.WillFinishLaunching((_,__) => {
                CrossFirebase.Initialize(CreateCrossFirebaseSettings());
                return false;
            }));
#elif ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) => {
                CrossFirebase.Initialize(activity, () => Platform.CurrentActivity, CreateCrossFirebaseSettings());
            }));
#endif
        });

		builder.Services.AddSingleton(_ => CrossFirebaseAuth.Current);
        RunSessionValidation();

        return builder;
    }

    private static CrossFirebaseSettings CreateCrossFirebaseSettings()
    {
        CrossFirebaseSettings settings = new(
            isAnalyticsEnabled: false,
            isAuthEnabled: true,
            isCloudMessagingEnabled: true,
            isDynamicLinksEnabled: false,
            isFirestoreEnabled: true,
            isFunctionsEnabled: true,
            isRemoteConfigEnabled: false,
            isStorageEnabled: true,
            googleRequestIdToken: "443931860533-lvbmdbnge1tdc2dmqpvmqg511ag25cv5.apps.googleusercontent.com");

        return settings;
    }

    private static void RunSessionValidation()
    {
        Task.Run(static () => 
        {
            // Load First View
            MainThread.BeginInvokeOnMainThread(static async () => 
            {
                if(Application.Current == null)
                {
                    return;
                }
                
                bool userSignedIn = await DatabaseManager.ValidateCurrentSession(); 
                await LocationManager.UpdateLocationAsync();
                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                CrossFirebaseCloudMessaging.Current.TokenChanged += Current_TokenChanged;
                //await ValidatePermissions(); 
                if (userSignedIn && SessionManager.CurrentSession != null
                    && SessionManager.CurrentSession.Client != null) 
                {
                    if(LocationManager.CurrentLocation != null)
                    {
                        await DatabaseManager.UpdateClientLocationAsync(SessionManager.CurrentSession.Client.UserID, new(LocationManager.CurrentLocation));
                    }
                    Application.Current.Windows[0].Page = new FP_MainShell(SessionManager.CurrentSession.Client);
                }
                else
                {
                    await DatabaseManager.LogOutAsync();
                    Application.Current.Windows[0].Page = new NavigationPage(new CP_Login());
                }
            }); 
        }); 
    }

    private static async void Current_TokenChanged(object? sender, Plugin.Firebase.CloudMessaging.EventArgs.FCMTokenChangedEventArgs e)
    {
        await DatabaseManager.UpdateCurrentUserFCMToken();
    }
}
