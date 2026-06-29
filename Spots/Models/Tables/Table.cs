using Plugin.Firebase.Firestore;

using eatMeet.Database;

namespace eatMeet.Models
{
    public class MemberMetadata
    {
        [FirestoreProperty(nameof(ReceiveTableNotifications))]
        public bool ReceiveTableNotifications { get; set; }

        public MemberMetadata()
        {
            ReceiveTableNotifications = true; // Default to true
        }

        public MemberMetadata(bool receiveTableNotifications)
        {
            ReceiveTableNotifications = receiveTableNotifications;
        }
    }

    public class Table
    {
        public List<string> SittingMembers {  get; set; }
        public string TableID { get; set; }
        public ImageSource TablePictureSource { get; set; }
        public string TableName { get; set; }
        public string Description { get; set; }
        public List<string> TableMembers { get; set; }
        public FirebaseLocation Location { get; set; }
        public Dictionary<string, MemberMetadata> MemberData { get; set; }
        public int OnlineCount
        {
            get
            {
                return SittingMembers.Count;
            }
        }

        public string TableStateColor
        {
            get
            {
                return OnlineCount > 0 ? "Green" : "Red";
            }
        }

        public Table()
        {
            TableID = "";
            SittingMembers = [];
            TablePictureSource = ImageSource.FromFile("logolong.png");
            TableName = "";
            Description = "";
            TableMembers = [];
            Location = new();
            MemberData = new Dictionary<string, MemberMetadata>();
        }

        public Table(Table_Firebase table, ImageSource tablePictureSource)
        {
            TableID = table.TableID;
            SittingMembers = [.. table.OnlineMembers];
            TablePictureSource = tablePictureSource;
            TableName = table.TableName;
            Description = table.Description;
            TableMembers = [.. table.TableMembers];
            Location = table.Location;
            MemberData = table.MemberData != null 
                ? new Dictionary<string, MemberMetadata>(table.MemberData) 
                : new Dictionary<string, MemberMetadata>();
        }

        public async Task UpdateTablePicture(string address)
        {
            if (address.Length > 0)
            {
                string downloadAddress = await DatabaseManager.GetImageDownloadLink(address);
                Uri imageUri = new(downloadAddress);

                TablePictureSource = ImageSource.FromUri(imageUri);
            }
            else
            {
                ImageSource.FromFile("logolong.png");
            }
        }
    }

    public class Table_Firebase
    {
        [FirestoreDocumentId]
        public string TableID { get; set; }

        [FirestoreProperty(nameof(OnlineMembers))]
        public IList<string> OnlineMembers { get; set; }

        [FirestoreProperty(nameof(TablePictureAddress))]
        public string TablePictureAddress { get; set; }

        [FirestoreProperty(nameof(TableName))]
        public string TableName { get; set; }

        [FirestoreProperty(nameof(Description))]
        public string Description { get; set; }

        [FirestoreProperty(nameof(TableMembers))]
        public IList<string> TableMembers { get; set; }

        [FirestoreProperty(nameof(Location))]
        public FirebaseLocation Location { get; set; }

        [FirestoreProperty(nameof(MemberData))]
        public IDictionary<string, MemberMetadata> MemberData { get; set; }

        public Table_Firebase()
        {
            TableID = string.Empty;
            OnlineMembers = [];
            TablePictureAddress = string.Empty;
            TableName = string.Empty;
            Description = string.Empty;
            TableMembers = [];
            Location = new();
            MemberData = new Dictionary<string, MemberMetadata>();
        }

        public Table_Firebase(Table table, string tablePictureAddress)
        {
            TableID = table.TableID;
            OnlineMembers = table.SittingMembers;
            TablePictureAddress = tablePictureAddress;
            TableName = table.TableName;
            Description = table.Description;
            TableMembers = table.TableMembers;
            Location = table.Location;
            MemberData = table.MemberData != null 
                ? new Dictionary<string, MemberMetadata>(table.MemberData) 
                : new Dictionary<string, MemberMetadata>();
        }

        public async Task<ImageSource> GetImageSource()
        {
            if (TablePictureAddress.Length > 0)
            {
                string downloadAddress = await DatabaseManager.GetImageDownloadLink(TablePictureAddress);
                Uri imageUri = new(downloadAddress);

                return ImageSource.FromUri(imageUri);
            }
            else
            {
                return ImageSource.FromFile("logolong.png");
            }
        }
    }
}
