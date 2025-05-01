using Plugin.Firebase.Firestore;
using System.ComponentModel;

using eatMeet.Database;
using eatMeet.Notifications;

namespace eatMeet.Models;

public class SpotPraise : INotifyPropertyChanged
{
    private ImageSource? _AuthorProfilePicture;
    private ImageSource? _SpotProfilePicture;
    private int _LikesCount = 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string PraiseID { get; private set; }
    public string AuthorID { get; private set; }
    public string AuthorFullName { get; private set; }
    public ImageSource? AuthorProfilePicture
    {
        get
        {
            return _AuthorProfilePicture ?? ImageSource.FromFile("logolong.png");
        }

        private set
        {
            _AuthorProfilePicture = value;
        }
    }
    public string SpotID { get; private set; }
    public string SpotFullName { get; private set; }
    public ImageSource? SpotProfilePicture
    {
        get
        {
            return _SpotProfilePicture ?? ImageSource.FromFile("logolong.png");
        }

        private set
        {
            _SpotProfilePicture = value;
        }
    }
    public FirebaseLocation SpotLocation { get; private set; }
    public DateTimeOffset CreationDate { get; set; }
    public string Comment { get; set; }
    public ImageSource? AttachedPicture { get; set; }
    public List<string> Likes { get; private set; }
    public int LikesCount {
        get => _LikesCount;
        set
        {
            _LikesCount = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LikeColor)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LikesCount)));
        }
    }
    public string LikeColor
    {
        get
        {
            return Likes.Contains(SessionManager.CurrentSession?.Client?.UserID ?? "NULL") ? "#FFA500" : "#6E6E6E";
        }
    }

    public SpotPraise(SpotPraise_Firebase spotPraise_FB, Client author, Spot spot, ImageSource? attachment = null)
    {
        PraiseID = spotPraise_FB.PraiseID;
        AuthorID = spotPraise_FB.AuthorID;
        AuthorFullName = author.FullName;
        AuthorProfilePicture = author.ProfilePictureSource;
        SpotID = spotPraise_FB.SpotID;
        SpotFullName = spot.Name + " - " + spot.Location.Address;
        SpotProfilePicture = spot.ProfilePictureSource;
        SpotLocation = spotPraise_FB.SpotLocation;
        CreationDate = spotPraise_FB.CreationDate;
        Comment = spotPraise_FB.Comment;
        AttachedPicture = attachment;
        Likes = [.. spotPraise_FB.Likes];
        LikesCount = spotPraise_FB.LikesCount;
    }

    public SpotPraise(string praiseID,
        string author,
        string authorFullName,
        string spotReviewed,
        string spotFullName,
        DateTimeOffset creationDate,
        ImageSource? authorPicture = null,
        ImageSource? spotPicture = null,
        FirebaseLocation? spotLocation = null,
        string comment = "",
        ImageSource? attachedPicture = null,
        List<string>? likes = null,
        int likesCount = 0)
    {
        PraiseID = praiseID;
        AuthorID = author;
        AuthorFullName = authorFullName;
        AuthorProfilePicture = authorPicture;
        SpotID = spotReviewed;
        SpotFullName = spotFullName;
        SpotProfilePicture = spotPicture;
        SpotLocation = spotLocation ?? new();
        CreationDate = creationDate;
        Comment = comment;
        AttachedPicture = attachedPicture;
        Likes = likes ?? [];
        LikesCount = likesCount;
    }

    public static async Task<SpotPraise> GetPraiseFromFirebaseObject(SpotPraise_Firebase praise)
    {
        Client author = await DatabaseManager.GetClientDataAsync(praise.AuthorID);
        Spot spot = await DatabaseManager.GetSpotDataAsync(praise.SpotID);
        ImageSource? attachment = null;

        if (praise.AttachedPictureAddress.Length > 0)
        {
            string downloadAddress = await DatabaseManager.GetImageDownloadLink(praise.AttachedPictureAddress);
            Uri imageUri = new(downloadAddress);

            attachment = ImageSource.FromUri(imageUri);
        }

        return new(praise, author, spot, attachment);
    }

    public static async Task<List<SpotPraise>> GetPraisesFromFirebaseObject(List<SpotPraise_Firebase> praises_Firebase, Client? author = null, Spot? spot = null)
    {
        List<SpotPraise> spotPraises = [];

        foreach (var praise_Firebase in praises_Firebase)
        {
            if (praise_Firebase != null)
            {
                SpotPraise_Firebase praise = praise_Firebase;
                Client managed_author = author != null ? author : await DatabaseManager.GetClientDataAsync(praise.AuthorID);
                Spot managed_spot = spot != null ? spot : await DatabaseManager.GetSpotDataAsync(praise.SpotID);
                ImageSource? attachment = null;

                if (praise.AttachedPictureAddress.Length > 0)
                {
                    string downloadAddress = await DatabaseManager.GetImageDownloadLink(praise.AttachedPictureAddress);
                    Uri imageUri = new(downloadAddress);

                    attachment = ImageSource.FromUri(imageUri);
                }

                spotPraises.Add(new(praise, managed_author, managed_spot, attachment));
            }
        }

        return spotPraises;
    }

    public async Task<bool?> LikeSwitch(string clientID)
    {
        return await DatabaseManager.Transaction_UpdateLikeOnSpotPraise(clientID, this);
    }
}

public class SpotPraise_Firebase
{
    [FirestoreDocumentId]
    public string PraiseID { get; set; }

    [FirestoreProperty(nameof(SearchCriteria_ID))]
    public IList<string> SearchCriteria_ID { get; set; }

    [FirestoreProperty(nameof(AuthorID))]
    public string AuthorID { get; set; }

    [FirestoreProperty(nameof(AuthorID_Array))]
    public IList<string> AuthorID_Array { get { return [AuthorID]; } private set { } }

    [FirestoreProperty(nameof(SpotID))]
    public string SpotID { get; set; }

    [FirestoreProperty(nameof(SpotLocation))]
    public FirebaseLocation SpotLocation { get; set; }

    [FirestoreProperty(nameof(CreationDate))]
    public DateTimeOffset CreationDate { get; set; }

    [FirestoreProperty(nameof(Comment))]
    public string Comment { get; set; }

    [FirestoreProperty(nameof(AttachedPictureAddress))]
    public string AttachedPictureAddress { get; set; }

    [FirestoreProperty(nameof(Likes))]
    public IList<string> Likes { get; set; }

    [FirestoreProperty(nameof(LikesCount))]
    public int LikesCount { get; set; }

    public SpotPraise_Firebase()
    {
        PraiseID = "";
        SearchCriteria_ID = [];
        AuthorID = "";
        SpotID = "";
        SpotLocation = new();
        CreationDate = DateTimeOffset.Now;
        Comment = "";
        AttachedPictureAddress = "";
        Likes = [];
        LikesCount = 0;
    }

    public SpotPraise_Firebase(SpotPraise spotPraise, string attachmentAddress = "")
    {
        PraiseID = spotPraise.PraiseID;
        SearchCriteria_ID = [spotPraise.PraiseID];
        AuthorID = spotPraise.AuthorID;
        SpotID = spotPraise.SpotID;
        SpotLocation = spotPraise.SpotLocation;
        CreationDate = spotPraise.CreationDate;
        Comment = spotPraise.Comment;
        AttachedPictureAddress = attachmentAddress;
        Likes = spotPraise.Likes;
        LikesCount = spotPraise.LikesCount;
    }
}
