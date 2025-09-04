using eatMeet.CloudFunctions;
using eatMeet.Models;
using eatMeet.ResourceManager;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eatMeet.GooglePlacesService
{
    public static class GooglePlaces
    {
        private class RequestParameters_GetAllRestaurants
        {
            [JsonPropertyName("textQuery")]
            public string TextQuery { get; set; } = "";

            [JsonPropertyName("locationLatitude")]
            public double LocationLatitude { get; set; } = 0;

            [JsonPropertyName("locationLongitude")]
            public double LocationLongitude { get; set; } = 0;

            [JsonPropertyName("searchRadius")]
            public int SearchRadius { get; set; } = -1;

            [JsonPropertyName("pageSize")]
            public int PageSize { get; set; } = 10;

            [JsonPropertyName("pageToken")]
            public string PageToken { get; set; } = "";

            public RequestParameters_GetAllRestaurants() { }

            public string ToJson()
            {
                return JsonSerializer.Serialize(this);
            }
        }

        private static RequestParameters_GetAllRestaurants? inputParams;
        
        public static async Task<Spot?> GetPlaceDetails(string placeID)
        {
            string jsonInputParams = JsonSerializer.Serialize(new { placeID });
            CloudFunctionsManager.Response_GetPlaceDetails response = await CloudFunctionsManager.CallMapsGetPlaceDetailsFunction(jsonInputParams);
            return response.GetSpot();
        }

        /// <summary>
        /// Retrieves all restaurants based on the provided text query, location, search radius, and page size.
        /// </summary>
        /// <param name="textQuery"></param>
        /// <param name="location"></param>
        /// <param name="searchRadius"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static async Task<List<Spot>> GetAllRestaurants(string textQuery, Location location, int searchRadius, int? pageSize = null)
        {
            inputParams = new RequestParameters_GetAllRestaurants
            {
                TextQuery = textQuery,
                LocationLatitude = location.Latitude,
                LocationLongitude = location.Longitude,
                SearchRadius = searchRadius,
                PageSize = pageSize ?? 10, // Default to 10
            };

            CloudFunctionsManager.Response_GetAllRestaurants response = await CloudFunctionsManager.CallMapsGetAllRestaurantsFunction(inputParams.ToJson());
            if (!string.IsNullOrEmpty(response.Errors))
            {
                throw new Exception(response.Errors);
            }
            inputParams.PageToken = response.nextPageToken;

            return response.GetSpots();
        }

        /// <summary>
        /// Retrieves the next page of restaurants based on the provided page token.
        /// </summary>
        /// <returns></returns>
        public static async Task<List<Spot>> GetAllRestaurants_NextPage()
        {
            List<Spot> result = [];
            if (inputParams == null || inputParams.PageToken.Length == 0)
            {
                return result;
            }

            CloudFunctionsManager.Response_GetAllRestaurants response = await CloudFunctionsManager.CallMapsGetAllRestaurantsFunction(inputParams.ToJson());
            if (!string.IsNullOrEmpty(response.Errors))
            {
                throw new Exception(response.Errors);
            }

            return response.GetSpots();
        }
    }
}
