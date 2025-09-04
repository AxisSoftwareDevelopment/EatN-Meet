using Plugin.Firebase.Firestore;
using System.ComponentModel;

using eatMeet.Database;

namespace eatMeet.Models;
public class Spot : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    #region Private Parameters
    private string? _SpotID;
    private string? _Name;
    private ImageSource? _profilePictureSource;
    private string? _profilePictureAddress;
    private FirebaseLocation? _location;
    private int? _praiseCount;
    #endregion

    #region Public Parameters
    public string SpotID
    {
        get => _SpotID ?? "";
        set
        {
            _SpotID = value ?? "";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpotID)));
        }
    }
    public ImageSource ProfilePictureSource
    {
        get => _profilePictureSource ?? ImageSource.FromFile("logolong.png");
        set
        {
            _profilePictureSource = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfilePictureSource)));
        }
    }
    public string? ProfilePictureAddress
    {
        get => _profilePictureAddress;
        set
        {
            _profilePictureAddress = value;
            ProfilePictureSource = string.IsNullOrEmpty(value) ? ImageSource.FromFile("logolong.png") : ImageSource.FromUri(new(value));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfilePictureAddress)));
        }
    }
    public string FullName
    {
        get => Name + " - " + Location.Address;
    }
    public string Name
    {
        get => _Name ?? "";
        set
        {
            _Name = value.Equals("") ? null : value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName)));
        }
    }
    public FirebaseLocation Location
    {
        get => _location ?? new FirebaseLocation();
        set
        {
            _location = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Geolocation)));
        }
    }
    public Location Geolocation
    {
        get => new(Location.Latitude, Location.Longitude);
        set
        {
            Location = new FirebaseLocation(Location.Address,
                value.Latitude,
                value.Longitude);
        }
    }
    public int PraiseCount
    {
        get => _praiseCount ?? 0;
        set
        {
            _praiseCount = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PraiseCount)));
        }
    }
    #endregion

    public Spot()
    {
        Name = "";
        Location = new FirebaseLocation();
        PraiseCount = 0;
    }

    public Spot(string userID, string name, ImageSource? profilePictureSource = null, string? profilePictureAddress = null,
        string phoneNumber = "", string phoneCountryCode = "", string description = "", FirebaseLocation? location = null, int? praiseCount = null)
    {
        SpotID = userID;
        Name = name;
        if (profilePictureAddress != null)
        {
            ProfilePictureAddress = profilePictureAddress;
        }
        else
        {
            ProfilePictureSource = profilePictureSource ?? ImageSource.FromFile("logolong.png");
        }
        Location = location ?? new FirebaseLocation();
        PraiseCount = praiseCount ?? 0;
    }

    public Spot(Spot_Firebase spotData, ImageSource profilePictureSource)
    {
        SpotID = spotData.SpotID;
        Name = spotData.Name;
        ProfilePictureSource = profilePictureSource;
        Location = spotData.Location;
        PraiseCount = spotData.PraiseCount;
    }

    public void UpdateUserData(Spot userData)
    {
        Name = userData.Name;
        ProfilePictureSource = userData.ProfilePictureSource;
        Location = userData.Location;
        PraiseCount = userData.PraiseCount;
    }

    public async Task UpdateProfilePicture(string firebaseAddress)
    {
        if (firebaseAddress.Length > 0)
        {
            string downloadAddress = await DatabaseManager.GetImageDownloadLink(firebaseAddress);
            Uri imageUri = new(downloadAddress);

            ProfilePictureSource = ImageSource.FromUri(imageUri);
        }
        else
        {
            ProfilePictureSource = ImageSource.FromFile("logolong.png");
        }
    }
}

public class Spot_Firebase
{
    [FirestoreDocumentId]
    public string SpotID { get; set; }
    [FirestoreProperty(nameof(Name))]
    public string Name { get; set; }
    [FirestoreProperty(nameof(Location))]
    public FirebaseLocation Location { get; set; }
    [FirestoreProperty(nameof(ProfilePictureAddress))]
    public string ProfilePictureAddress { get; set; }
    [FirestoreProperty(nameof(PraiseCount))]
    public int PraiseCount { get; set; }
    [FirestoreProperty(nameof(SearchTerms))]
    public IList<string> SearchTerms { get; set; }

    public Spot_Firebase(string spotID,
        string name,
        FirebaseLocation location,
        string profilePictureAddress,
        int praiseCount,
        List<string>? searchTerms = null)
    {
        SpotID = spotID;
        Name = name;
        Location = location;
        ProfilePictureAddress = profilePictureAddress;
        PraiseCount = praiseCount;
        SearchTerms = searchTerms ?? GenerateSearchTerms(name, location.Address);
    }

    public Spot_Firebase()
    {
        SpotID = "";
        Name = "";
        Location = new();
        ProfilePictureAddress = "";
        PraiseCount = 0;
        SearchTerms = [];
    }

    public Spot_Firebase(Spot spotData, string profilePictureAddress)
    {
        SpotID = spotData.SpotID;
        Name = spotData.Name;
        Location = spotData.Location;
        ProfilePictureAddress = profilePictureAddress;
        PraiseCount = spotData.PraiseCount;
        string curatedAddress = Location.Address.Trim([' ', ',', '#']);
        SearchTerms = GenerateSearchTerms(Name, curatedAddress);
    }

    private List<string> GenerateSearchTerms(string spotName, string address)
    {
        List<string> retVal = [];
        List<string> composedTerms = [];

        foreach (string word in spotName.Split(' ').Concat(address.Split(' ')))
        {
            string currentTerm = "";
            foreach (char letter in word)
            {
                currentTerm += char.ToUpper(letter);
                retVal.Add(currentTerm);

                foreach (string term in composedTerms)
                {
                    retVal.Add(term + " " + currentTerm);
                }
            }
            composedTerms.Add(currentTerm);
        }

        return retVal.Concat(composedTerms).ToList();
    }

    public async Task<ImageSource> GetImageSource()
    {
        if (ProfilePictureAddress.Length > 0)
        {
            string downloadAddress = await DatabaseManager.GetImageDownloadLink(ProfilePictureAddress);
            Uri imageUri = new(downloadAddress);

            return ImageSource.FromUri(imageUri);
        }
        else
        {
            return ImageSource.FromFile("logolong.png");
        }
    }
}