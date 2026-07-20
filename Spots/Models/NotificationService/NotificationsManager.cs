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

        public static async Task SendTableSitNotification(Table table, string userWhoSat)
        {
            // Get FCM tokens of members who want notifications (excluding the user who sat)
            var tokensToNotify = await GetNotifiableMemberTokens(table, excludeUserId: userWhoSat);

            if (tokensToNotify.Count > 0)
            {
                await CloudMessagingManager.TriggerNotificationViaTokensAsync(
                    tokensToNotify,
                    $"{table.TableName}",
                    $"{await GetUserDisplayName(userWhoSat)} sat at the table"
                );
            }
        }

        public static async Task SendTableStandNotification(Table table, string userWhoStood)
        {
            // Get FCM tokens of members who want notifications (excluding the user who stood)
            var tokensToNotify = await GetNotifiableMemberTokens(table, excludeUserId: userWhoStood);

            if (tokensToNotify.Count > 0)
            {
                await CloudMessagingManager.TriggerNotificationViaTokensAsync(
                    tokensToNotify,
                    $"{table.TableName}",
                    $"{await GetUserDisplayName(userWhoStood)} left the table"
                );
            }
        }

        private static async Task<List<string>> GetNotifiableMemberTokens(Table table, string? excludeUserId = null)
        {
            List<string> fcmTokens = [];

            // Get all table members except the one who triggered the action
            var memberIds = table.TableMembers
                .Where(memberId => memberId != excludeUserId)
                .ToList();

            if (memberIds.Count == 0)
                return fcmTokens;

            // Fetch client data for all members
            List<Client> clients = await DatabaseManager.FetchClientsByID(memberIds);

            // Filter by notification preference and collect FCM tokens
            foreach (var client in clients)
            {
                // Check if user wants notifications for this table
                bool wantsNotifications = table.MemberData.TryGetValue(client.UserID, out var metadata)
                    ? metadata.ReceiveTableNotifications
                    : true; // Default to true if not set

                // Add token if user wants notifications and has a valid token
                if (wantsNotifications && !string.IsNullOrEmpty(client.FCMToken))
                {
                    fcmTokens.Add(client.FCMToken);
                }
            }

            return fcmTokens;
        }

        private static async Task<string> GetUserDisplayName(string userId)
        {
            // If it's the current user, use their cached name
            if (SessionManager.CurrentSession?.Client?.UserID == userId)
            {
                return SessionManager.CurrentSession.Client.FirstName;
            }

            // Otherwise fetch from database
            var clients = await DatabaseManager.FetchClientsByID([userId]);
            return clients.FirstOrDefault()?.FirstName ?? "Someone";
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
            // If there is no current user, skip fetching notifications to avoid Firestore permission errors
            var ownerID = SessionManager.CurrentSession?.Client?.UserID;
            if (string.IsNullOrEmpty(ownerID))
            {
                _notificationsCount = 0;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotificationsPageIcon)));
                return;
            }

            List<INotification> notifications = await DatabaseManager.FetchNotifications_Filtered(ownerID: ownerID);
            _notificationsCount = notifications.Count;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotificationsPageIcon)));
        }
    }
}
