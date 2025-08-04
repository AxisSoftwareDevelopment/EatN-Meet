using eatMeet.Database;
using eatMeet.Models;
using eatMeet.CloudMessaging;
using System.ComponentModel;

namespace eatMeet.Notifications
{
    public static class NotificationsManager
    {
        public static readonly NotificationsHandler Handler = new();

        public static Task SendTableInvitation(string FCMToken, string Sender, string TableName)
        {
            return CloudMessagingManager.TriggerNotificationViaTokensAsync([FCMToken], $"Table Invitation", $"{Sender} invites you to join the {TableName} table.");
        }
    }

    public class NotificationsHandler : INotifyPropertyChanged
    {
        // Currently only handles up to five
        private int _notificationsCount = 0;

        public NotificationsHandler() { }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSource NotificationsPageIcon
        {
            get
            {
                return _notificationsCount > 0 ? ImageSource.FromFile("iconnotificationsfilled.png") : ImageSource.FromFile("iconnotificationsempty.png");
            }
        }

        public async Task UpdateNotifications()
        {
            List<INotification> notifications = await DatabaseManager.FetchNotifications_Filtered(ownerID: SessionManager.CurrentSession?.Client?.UserID);
            _notificationsCount = notifications.Count;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotificationsPageIcon)));
        }
    }
}
