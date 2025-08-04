using eatMeet.Models;
using Plugin.Firebase.Functions;
using System.Text.Json.Serialization;

namespace eatMeet.CloudFunctions
{
    public static class CloudFunctionsManager
    {
        private const string TRIGGER_NOTIFICATION = "TRIGGER_NOTIFICATION";
        private const string MAPS_GET_ALL_RESTAURANTS = "MAPS_GET_ALL_RESTAURANTS";

        public class Response_GetAllRestaurants
        {
            [JsonPropertyName("output")]
            public List<PlaceInfo> Places { get; set; } = [];
            [JsonPropertyName("errors")]
            public string Errors { get; set; } = "";
            [JsonPropertyName("nextPageToken")]
            public string nextPageToken { get; set; } = "";

            public List<Spot> GetSpots()
            {
                List<Spot> retVal = [];
                if (Places.Count > 0)
                {


                    foreach (var place in Places)
                    {
                        Spot spot = new()
                        {
                            SpotID = place.Id,
                            Name = place.DisplayName.Text,
                            Description = place.FormattedAddress,
                        };
                        string? imageUri = place.Photos.FirstOrDefault()?.GoogleMapsUri;
                        if (imageUri != null)
                        {
                            spot.ProfilePictureSource = ImageSource.FromUri(new Uri(imageUri));
                        }
                        retVal.Add(spot);
                    }
                }
                return retVal;
            }
        }
        public class PlaceInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("formattedAddress")]
            public string FormattedAddress { get; set; } = "";

            [JsonPropertyName("displayName")]
            public DisplayName DisplayName { get; set; } = new();

            [JsonPropertyName("photos")]
            public List<PhotoInfo> Photos { get; set; } = new();
        }

        public class DisplayName
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = "";
        }

        public class PhotoInfo
        {
            [JsonPropertyName("googleMapsUri")]
            public string GoogleMapsUri { get; set; } = "";
        }

        public static Task CallNotificationFunction(string dataJson)
        {
            return CrossFirebaseFunctions.Current
                .GetHttpsCallable(TRIGGER_NOTIFICATION)
                .CallAsync(dataJson);
        }

        public static Task<Response_GetAllRestaurants> CallMapsGetAllRestaurantsFunction(string dataJson)
        {
            return CrossFirebaseFunctions.Current
                .GetHttpsCallable(MAPS_GET_ALL_RESTAURANTS)
                .CallAsync<Response_GetAllRestaurants>(dataJson);
        }
    }
}
