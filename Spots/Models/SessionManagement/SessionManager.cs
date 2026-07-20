using eatMeet.Database;
using eatMeet.Notifications;
using System.ComponentModel;

namespace eatMeet.Models;

public static class SessionManager
{
    public static UserSession? CurrentSession;

    public static async Task<bool> StartSessionAsync(Client user)
    {
        if (CurrentSession == null)
        {
            CurrentSession = new(user);

            // Update notifications for the newly-started session (requires CurrentSession to be set)
            await NotificationsManager.Handler.UpdateNotifications();

            return true;
        }
        else
        {
            return false;
        }
    }

    public static void CloseSession( bool shouldUpdateMainPage = true)
    {
        if (Application.Current != null)
        {
            CurrentSession = null;
            FirebaseLocation? location = LocationManager.CurrentLocation != null ?
                new FirebaseLocation(LocationManager.CurrentLocation) : null;
            Task.Run(async () => { await DatabaseManager.LogOutAsync(location); });

            if (shouldUpdateMainPage)
            {
                Application.Current.Windows[0].Page = new NavigationPage(new CP_Login());
            }
        }
    }
}

public class UserSession : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Client? Client {  get; set; }
    public UserSession(Client user)
    {
        Client = user;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Client)));
    }

    public void UpdateUserData(Client user)
    {
        if (Client != null)
        {
            Client.UpdateUserData(user);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Client)));
        }
    }
}
