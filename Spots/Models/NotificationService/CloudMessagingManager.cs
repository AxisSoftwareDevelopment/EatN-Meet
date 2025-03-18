using Plugin.Firebase.CloudMessaging;
using System.Text.Json.Serialization;
using System.Text.Json;

using eatMeet.CloudFunctions;
using Plugin.Firebase.CloudMessaging.EventArgs;

namespace eatMeet.CloudMessaging
{
    public static class CloudMessagingManager
    {
        public static Task<string> GetFCMTokenAsync()
        {
            return CrossFirebaseCloudMessaging.Current.GetTokenAsync();
        }

        public static Task SubscribeToTopicAsync(string topic)
        {
            return CrossFirebaseCloudMessaging.Current.SubscribeToTopicAsync(topic);
        }

        public static Task UnsubscribeFromTopicAsync(string topic)
        {
            return CrossFirebaseCloudMessaging.Current.UnsubscribeFromTopicAsync(topic);
        }
        public static Task TriggerNotificationViaTokensAsync(IEnumerable<string> tokens, string title, string body)
        {
            return CloudFunctionsManager.CallNotificationFunction(PushNotification.FromTokens(tokens, title, body).ToJson());
        }

        public static Task TriggerNotificationViaTopicAsync(string topic, string title, string body)
        {
            return CloudFunctionsManager.CallNotificationFunction(PushNotification.FromTopic(topic, title, body).ToJson());
        }
    }

    public class PushNotification
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PushNotificationType Type { get; private set; }

        [JsonPropertyName("title")]
        public string Title { get; private set; }

        [JsonPropertyName("body")]
        public string Body { get; private set; }

        [JsonPropertyName("fcm_tokens")]
        public IEnumerable<string> FcmTokens { get; private set; }

        [JsonPropertyName("topic")]
        public string Topic { get; private set; }

        public static PushNotification FromTokens(IEnumerable<string> fcmTokens, string title, string body)
        {
            return new PushNotification(PushNotificationType.TOKENS, fcmTokens: fcmTokens, title: title, body: body);
        }

        public static PushNotification FromTopic(string topic, string title, string body)
        {
            return new PushNotification(PushNotificationType.TOPIC, topic: topic, title: title, body: body);
        }

        private PushNotification(
            PushNotificationType type,
            IEnumerable<string>? fcmTokens = null,
            string? topic = null,
            string? title = null,
            string? body = null)
        {
            Type = type;
            FcmTokens = fcmTokens?? [];
            Topic = topic ?? "";
            Title = title ?? "";
            Body = body ?? "";
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    public enum PushNotificationType
    {
        TOKENS, TOPIC
    }
}
