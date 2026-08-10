using Plugin.Firebase.Firestore;
using System.ComponentModel;

using eatMeet.Database;
using eatMeet.Utilities;

namespace eatMeet.Models;
public class Client : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    #region Private Parameters
    private string? _UserID;
    private string? _FirstName;
    private string? _LastName;
    private DateTimeOffset _BirthDate;
    private string? _Email;
    private string? _PhoneNumber;
    private string? _PhoneCountryCode;
    private string? _Description;
    private ImageSource? _ProfilePictureSource;
    private FirebaseLocation? _LastLocation;
    private List<string>? _Followers;
    private List<string>? _Followed;
    private int? _LikesCount;
    private string? _FCMToken;
    #endregion

    #region Public Parameters
    public bool UserDataRetrieved
    {
        get => FirstName.Length > 0;
    }
    public ImageSource ProfilePictureSource
    {
        get => _ProfilePictureSource ?? ImageSource.FromFile("logoshort.png");
        set
        {
            _ProfilePictureSource = value ?? ImageSource.FromFile("logoshort.png");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfilePictureSource)));
        }
    }
    public string FullPhoneNumber
    {
        get => (PhoneNumber?.Length < 10 || PhoneCountryCode?.Length < 2) ? "+ -- --- --- ----" : $"+({_PhoneCountryCode}) {PhoneNumber?[..3]} {PhoneNumber?.Substring(3, 3)} {PhoneNumber?.Substring(6, 4)}";
    }
    public string FullName
    {
        get => $"{FirstName} {LastName}"; 
    }
    public string UserID
    {
        get => _UserID ?? "";
        set
        {
            _UserID = value ?? "";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserID)));
        }
    }
    public string FirstName
    {
        get => _FirstName ?? "";
        set
        {
            _FirstName = value ?? "";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FirstName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName)));
        }
    }
    public string LastName
    {
        get => _LastName ?? ""; 
        set
        {
            _LastName = value ?? "";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName)));
        }
    }
    public DateTimeOffset BirthDate
    {
        get => _BirthDate;
        set
        {
            _BirthDate = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BirthDate)));
        }
    }
    public string Email
    {
        get => _Email ?? "";
        set
        {
            _Email = value ?? "";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Email)));
        }
    }
    public string PhoneNumber
    {
        get => _PhoneNumber ?? "";
        set
        {
            _PhoneNumber = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PhoneNumber)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullPhoneNumber)));
        }
    }
    public string PhoneCountryCode
    {
        get => _PhoneCountryCode ?? "";
        set
        {
            _PhoneCountryCode = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PhoneCountryCode)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullPhoneNumber)));
        }
    }
    public string Description
    {
        get => _Description ?? "";
        set
        {
            _Description = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
        }
    }
    public List<string> Followers
    {
        get => _Followers ?? [];
        set
        {
            _Followers = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Followers)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FollowersCount)));
        }
    }
    public int FollowersCount
    {
        get => _Followers?.Count ?? 0;
    }
    public List<string> Followed
    {
        get => _Followed ?? [];
        set
        {
            _Followed = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Followed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FollowedCount)));
        }
    }
    public int FollowedCount
    {
        get => _Followed?.Count ?? 0;
    }
    public FirebaseLocation? LastLocation
    {
        get => _LastLocation;
        set
        {
            if (value == null)
            {
                _LastLocation = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastLocation)));
            }
        }
    }
    public int LikesCount
    {
        get => _LikesCount ?? 0;
        set
        {
            _LikesCount = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LikesCount)));
        }
    }
    public string FCMToken
    {
        get => _FCMToken ?? "";
        set
        {
            _FCMToken = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FCMToken)));
        }
    }
    #endregion
    public Client()
    {
        UserID = "";
        FirstName = "";
        LastName = "";
        BirthDate = DateTimeOffset.Now;
        Email = "";
        PhoneCountryCode = "";
        PhoneNumber = "";
        Description = "";
        Followers = [];
        Followed = [];
        LastLocation = null;
        LikesCount = 0;
    }

    public Client(string userID, string firstName, string lastName, DateTimeOffset birthDate, string mail, 
        ImageSource? profilePictureSrc = null, string phoneNumber = "", string phoneCountryCode = "", string description = "",
        List<string>? followers = null, List<string>? followed = null, FirebaseLocation? lastLocation = null, int likesCount = 0)
    {
        UserID = userID;
        FirstName = firstName;
        LastName = lastName;
        BirthDate = birthDate;
        Email = mail;
        ProfilePictureSource = profilePictureSrc ?? ImageSource.FromFile("logoshort.png");
        PhoneNumber = phoneNumber;
        PhoneCountryCode = phoneCountryCode;
        Description = description;
        Followers = followers ?? [];
        Followed = followed ?? [];
        LastLocation = lastLocation;
        LikesCount = likesCount;
    }

    public Client(Client_Firebase firebaseData, ImageSource profilePictureSrc)
    {
        UserID = firebaseData.UserID;
        FirstName = firebaseData.FirstName;
        LastName = firebaseData.LastName;
        BirthDate = firebaseData.BirthDate;
        Email = firebaseData.Email;
        ProfilePictureSource = profilePictureSrc;
        PhoneNumber = firebaseData.PhoneNumber;
        PhoneCountryCode = firebaseData.PhoneCountryCode;
        Description = firebaseData.Description;
        LikesCount = firebaseData.LikesCount;
        FCMToken = firebaseData.FCMToken;
        LastLocation = (firebaseData.LastLocation.Address.Length == 0
            && firebaseData.LastLocation.Latitude == 0
            && firebaseData.LastLocation.Longitude == 0)
            ? null : firebaseData.LastLocation;
        Followers = [.. firebaseData.Followers];
        Followed = [.. firebaseData.Followed];
    }

    public void UpdateUserData(Client userData)
    {
        UserID = userData.UserID;
        FirstName = userData.FirstName;
        LastName = userData.LastName;
        BirthDate = userData.BirthDate;
        Email = userData.Email;
        ProfilePictureSource = userData.ProfilePictureSource;
        PhoneNumber = userData.PhoneNumber;
        PhoneCountryCode = userData.PhoneCountryCode;
        Description = userData.Description;
        Followers = userData.Followers;
        Followed = userData.Followed;
        LastLocation = userData.LastLocation;
    }

    public async Task UpdateProfilePicture(string address)
    {
        ProfilePictureSource = await ImageSourceResolver.ResolveAsync(address, "logoshort.png");
    }

    public async Task OpenClientView(INavigation? navigation)
    {
        if (navigation != null)
        {
            bool bFollowing = UserID != SessionManager.CurrentSession?.Client?.UserID
                && Followers.Contains(SessionManager.CurrentSession?.Client?.UserID ?? "NULL");

            await navigation.PushAsync(new CP_UserProfile(this, bFollowing));
        }
    }
}

public class Client_Firebase
{
    [FirestoreDocumentId]
    public string UserID { get; set; }

    [FirestoreProperty(nameof(FirstName))]
    public string FirstName { get; set; }

    [FirestoreProperty(nameof(LastName))]
    public string LastName { get; set; }

    [FirestoreProperty(nameof(BirthDate))]
    public DateTimeOffset BirthDate { get; set; }

    [FirestoreProperty(nameof(Email))]
    public string Email { get; set; }

    [FirestoreProperty(nameof(PhoneNumber))]
    public string PhoneNumber { get; set; }

    [FirestoreProperty(nameof(PhoneCountryCode))]
    public string PhoneCountryCode { get; set; }

    [FirestoreProperty(nameof(Description))]
    public string Description { get; set; }

    [FirestoreProperty(nameof(ProfilePictureAddress))]
    public string ProfilePictureAddress { get; set; }

    [FirestoreProperty(nameof(SearchTerms))]
    public IList<string> SearchTerms { get; set; }

    [FirestoreProperty(nameof(Followers))]
    public IList<string> Followers { get; set; }

    [FirestoreProperty(nameof(Followed))]
    public IList<string> Followed { get; set; }

    [FirestoreProperty(nameof(LastLocation))]
    public FirebaseLocation LastLocation { get; set; }

    [FirestoreProperty(nameof(ClientID_ForSearch))]
    public IList<string> ClientID_ForSearch { get; set; }

    [FirestoreProperty(nameof(LikesCount))]
    public int LikesCount { get; set; }

    [FirestoreProperty(nameof(FCMToken))]
    public string FCMToken { get; set; }


    public Client_Firebase()
    {
        UserID = "";
        FirstName = "";
        LastName = "";
        BirthDate = DateTimeOffset.Now;
        Email = "";
        PhoneNumber = "";
        PhoneCountryCode = "";
        Description = "";
        ProfilePictureAddress = "";
        SearchTerms = [];
        Followers = [];
        Followed = [];
        ClientID_ForSearch = [];
        LastLocation = new("", 0, 0);
        LikesCount = 0;
        FCMToken = "";
    }

    public Client_Firebase(Client userData, string profilePictureAddress)
    {
        UserID = userData.UserID;
        FirstName = userData.FirstName;
        LastName = userData.LastName;
        BirthDate = userData.BirthDate;
        Email = userData.Email;
        PhoneNumber = userData.PhoneNumber;
        PhoneCountryCode = userData.PhoneCountryCode;
        Description = userData.Description;
        ProfilePictureAddress = profilePictureAddress;
        SearchTerms = GenerateSearchTerms(FirstName, LastName);
        Followers = userData.Followers;
        Followed = userData.Followed;
        LastLocation = userData.LastLocation ?? new("", 0, 0);
        ClientID_ForSearch = [userData.UserID];
        LikesCount = userData.LikesCount;
        FCMToken = userData.FCMToken;
    }

    private static List<string> GenerateSearchTerms(string firstName, string lastName)
    {
        List<string> retVal = [];
        List<string> composedTerms = [];

        foreach (string word in firstName.Split(' ').Concat(lastName.Split(' ')))
        {
            string currentTerm = "";
            foreach (char letter in word)
            {
                currentTerm += char.ToUpper(letter);
                retVal.Add(currentTerm);

                AttachComposedStrings(ref retVal, composedTerms.Concat([currentTerm]).ToList());
                foreach (string term in composedTerms)
                {
                    retVal.Add(term + " " + currentTerm);
                }
            }
            composedTerms.Add(currentTerm);
        }

        return retVal.Concat(composedTerms).ToList();
    }

    private static void AttachComposedStrings(ref List<string> retVal, List<string> composedTermsLeft, string currentTerm = "")
    {
        if(composedTermsLeft.Count > 0)
        {
            foreach (string term in composedTermsLeft)
            {
                AttachComposedStrings(ref retVal, composedTermsLeft.Where(compTerm => !compTerm.Equals(term)).ToList(), currentTerm + " " + term);
            }
        }
        else
        {
            retVal.Add(currentTerm.Trim());
        }
    }

    public Task<ImageSource> GetImageSource()
    {
        return ImageSourceResolver.ResolveAsync(ProfilePictureAddress, "logoshort.png");
    }
}
