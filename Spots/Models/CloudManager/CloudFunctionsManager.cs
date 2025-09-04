using eatMeet.Models;
using Plugin.Firebase.Functions;
using System.Text.Json.Serialization;

namespace eatMeet.CloudFunctions
{
    public static class CloudFunctionsManager
    {
        private const string TRIGGER_NOTIFICATION = "TRIGGER_NOTIFICATION";
        private const string MAPS_GET_ALL_RESTAURANTS = "MAPS_GET_ALL_RESTAURANTS";
        private const string MAPS_GET_PLACE_DETAILS = "MAPS_GET_PLACE_DETAILS";

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
                        Spot? spot = place.GetSpot();
                        if (spot != null)
                        {
                            retVal.Add(spot);
                        }
                    }
                }
                return retVal;
            }
        }
        public class Response_GetPlaceDetails
        {
            [JsonPropertyName("output")]
            public PlaceInfo PlaceDetails { get; set; } = new();
            [JsonPropertyName("errors")]
            public string Errors { get; set; } = "";
            [JsonPropertyName("nextPageToken")]
            public string nextPageToken { get; set; } = "";

            public Spot? GetSpot()
            {
                return PlaceDetails.GetSpot();
            }
        }
        public class PlaceInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("formattedAddress")]
            public string FormattedAddress { get; set; } = "";
            [JsonPropertyName("location")]
            public JsonLocation Location { get; set; } = new();

            [JsonPropertyName("displayName")]
            public DisplayName DisplayName { get; set; } = new();

            [JsonPropertyName("photos")]
            public List<PhotoInfo> Photos { get; set; } = new();

            [JsonPropertyName("profilePicture")]
            public string ProfilePicture { get; set; } = "";

            public Spot? GetSpot()
            {
                Spot? retVal = null;

                if(!string.IsNullOrEmpty(Id))
                {
                    Spot spot = new()
                    {
                        SpotID = Id,
                        Name = DisplayName.Text,
                        Location = new FirebaseLocation
                        {
                            Latitude = Location.Latitude,
                            Longitude = Location.Longitude,
                            Address = FormattedAddress
                        },
                    };
                    string? imageUri = ProfilePicture.Length > 0 ? ProfilePicture : null;
                    if (imageUri != null)
                    {
                        spot.ProfilePictureAddress = imageUri;
                    }
                    retVal = spot;
                }

                return retVal;
            }
        }
        public class JsonLocation
        {
            [JsonPropertyName("latitude")]
            public double Latitude { get; set; } = 0;
            [JsonPropertyName("longitude")]
            public double Longitude { get; set; } = 0;
        }

        public class DisplayName
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = "";
        }

        public class PhotoInfo
        {
            [JsonPropertyName("name")]
            public string photoName { get; set; } = "";

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

        public static Task<Response_GetPlaceDetails> CallMapsGetPlaceDetailsFunction(string dataJson)
        {
            return CrossFirebaseFunctions.Current
                .GetHttpsCallable(MAPS_GET_PLACE_DETAILS)
                .CallAsync<Response_GetPlaceDetails>(dataJson);
        }
    }
}
