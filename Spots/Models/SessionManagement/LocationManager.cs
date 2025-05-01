using Geohash;
using Plugin.Firebase.Firestore;

using eatMeet.ResourceManager;
using eatMeet.Utilities;

namespace eatMeet.Models;

public static class LocationManager
{
    public static Location? CurrentLocation { get; private set; }
    public static Geohasher Encoder { get; private set; } = new();

    public static async Task<Location?> GetUpdatedLocaionAsync()
    {
        CurrentLocation = await GetLocation();
        return CurrentLocation;
    }

    public static async Task UpdateLocationAsync()
    {
        CurrentLocation = await GetLocation();
    }

    public static async Task<List<Location>> GetLocationsFromAddress(string address)
    {
        List<Location> retVal = [];

        try
        {
            IEnumerable<Location> locations = await Geocoding.GetLocationsAsync(address);
            retVal.AddRange(locations);
        }
        catch (Exception ex)
        {
            if (Application.Current != null)
            {
                string[] stringResources = ResourceManagement.GetStringResources(Application.Current.Resources, ["lbl_Error", "lbl_GeolocationError", "lbl_Ok"]);
                await UserInterface.DisplayPopUp_Regular(stringResources[0], ex.Message + "\n" + stringResources[1], stringResources[2]);
            }
        }

        return retVal;
    }

    public static async Task<List<ListItemAddress>> GetAddressesFromAddress(string originalAddress)
    {
        List<ListItemAddress> retVal = [];
        try
        {
            List<Location> locations = await GetLocationsFromAddress(originalAddress);
            foreach(Location location in locations)
            {
                IEnumerable<Placemark> addresses = await Geocoding.GetPlacemarksAsync(location);
                foreach (Placemark address in addresses)
                {
                    string formattedAddress = GetAddressFromPlacemark(address);
                    retVal.Add(new ListItemAddress(formattedAddress, address.Location));
                }
            }
        }
        catch (Exception ex)
        {
            if (Application.Current != null)
            {
                string[] stringResources = ResourceManagement.GetStringResources(Application.Current.Resources, ["lbl_Error", "lbl_GeolocationError", "lbl_Ok"]);
                await UserInterface.DisplayPopUp_Regular(stringResources[0], ex.Message + "\n" + stringResources[1], stringResources[2]);
            }
        }
        return retVal;
    }

    private static async Task<Location?> GetLocation()
    {
        Location? location;
        try
        {
            location = await Geolocation.Default.GetLocationAsync();
            if (location == null)
            {
                location = await Geolocation.Default.GetLocationAsync();
            }
        }
        catch (PermissionException)
        {
            bool permissionGranted = false;
            while (!permissionGranted)
            {
                PermissionStatus locationWhenInUse = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                permissionGranted = locationWhenInUse == PermissionStatus.Granted;
            }
            location = await GetLocation();
        }
        catch (FeatureNotEnabledException ex)
        {
            location = null;
            if (Application.Current != null)
            {
                string[] stringResources = ResourceManagement.GetStringResources(Application.Current.Resources, ["lbl_Error", "lbl_GeolocationDisabledError", "lbl_Ok"]);
                await UserInterface.DisplayPopUp_Regular(stringResources[0], ex.Message + "\n" + stringResources[1], stringResources[2]);
            }
        }
        catch
        {
            location = null;
            if(Application.Current != null)
            {
                string[] stringResources = ResourceManagement.GetStringResources(Application.Current.Resources, ["lbl_Error", "lbl_GeolocationError", "lbl_Ok"]);
                await UserInterface.DisplayPopUp_Regular(stringResources[0], stringResources[1], stringResources[2]);
            }
        }
        return location;
    }

    private static string GetAddressFromPlacemark(Placemark placemark)
    {
        // Extract the relevant properties from the Placemark object
        string street = placemark.Thoroughfare ?? ""; // Street name
        string number = placemark.SubThoroughfare ?? ""; // Street number
        string col = placemark.SubLocality ?? ""; // Neighborhood or subdivision
        string city = placemark.Locality ?? ""; // City
        string state = placemark.AdminArea ?? ""; // State or administrative area
        string country = placemark.CountryName ?? ""; // Country

        // Format the address string
        return $"{street} #{number}, {col}, {city}, {state}, {country}".Trim([',', ' ', '#']);
    }
}

public class FirebaseLocation : IFirestoreObject
{
    private double _Latitude = 0;
    private double _Longitude = 0;

    [FirestoreProperty(nameof(Address))]
    public string Address { get; set; }

    [FirestoreProperty(nameof(Latitude))]
    public double Latitude { get; set; }

    [FirestoreProperty(nameof(Longitude))]
    public double Longitude { get; set;}

    [FirestoreProperty(nameof(Geohash))]
    public List<string> Geohash
    {
        get { return [LocationManager.Encoder.Encode(_Latitude, _Longitude)]; }
        private set { }
    }

    public FirebaseLocation()
    {
        Address = "";
        Latitude = 0;
        Longitude = 0;
    }

    public FirebaseLocation(string addr, double lat, double lng)
    {
        Address = addr;
        Latitude = lat;
        Longitude = lng;
    }

    public FirebaseLocation(Location location)
    {
        Address = "";
        Latitude = location.Latitude;
        Longitude = location.Longitude;
    }
}

public class ListItemAddress
{
    public string Address { get; set; }
    public string LocationString { get; set; }
    public Location Location { get; set; }

    public ListItemAddress(string address, Location location)
    {
        Address = address;
        Location = location;
        LocationString = $"{location.Latitude}, {location.Longitude}";
    }
}

