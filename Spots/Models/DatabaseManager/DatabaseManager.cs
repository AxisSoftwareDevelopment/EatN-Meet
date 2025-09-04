using eatMeet.FirebaseStorage;
using eatMeet.Firestore;
using eatMeet.Models;
using eatMeet.ResourceManager;
using eatMeet.Utilities;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Core.Exceptions;
using Plugin.Firebase.Firestore;
using System.Reflection.Metadata;

namespace eatMeet.Database;
public static class DatabaseManager
{
    private const string COLLECTION_USER_DATA = "UserData";
    private const string COLLECTION_SPOTS = "Spots";
    private const string COLLECTION_PRAISES = "Praises";
    private const string COLLECTION_TABLES = "Tables";
    private const string COLLECTION_NOTIFICATIONS = "Notifications";

    #region Externalizing Storage Methods
    public static Task<string> GetImageDownloadLink(string path)
    {
        return FirebaseStorageManager.GetImageDownloadLink(path);
    }

    public static Task<string> SaveFile(string path, string fileName, ImageFile imageFile)
    {
        return FirebaseStorageManager.SaveFile(path, fileName, imageFile);
    }
    #endregion

    #region Session Management
    public static async Task<Client> LogInUserAsync(string email, string password, FirebaseLocation? lastLocation = null, bool getUser = true)
    {
        string[] userSignInMethods = await CrossFirebaseAuth.Current.FetchSignInMethodsAsync(email);

        if (userSignInMethods.Length == 0 || !userSignInMethods.Contains("password"))
            throw new FirebaseAuthException(FIRAuthError.InvalidEmail, "Custom Exception -> There was no 'email and password' login method, or none at all.");

        IFirebaseUser iFUser = await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(email, password, false);

        if (!iFUser.IsEmailVerified)
        {
            await CrossFirebaseAuth.Current.SignOutAsync();
            throw new FirebaseAuthException(FIRAuthError.UserDisabled, "Custon Exception -> Email not verified.");
        }

        Client user = await GetClientDataAsync(iFUser.Uid);
        if (!user.UserDataRetrieved)
        {
            return user;
        }

        await SessionManager.StartSessionAsync(user);

        if (lastLocation != null)
        {
            await UpdateClientLocationAsync(iFUser.Uid, lastLocation);
        }

        await UpdateCurrentUserFCMToken();

        return user;
    }

    public static Task ResendEmailVerificationAsync()
    {
        return CrossFirebaseAuth.Current.CurrentUser.SendEmailVerificationAsync();
    }

    public static async Task<bool> CreateUserAsync(string email, string password)
    {
        bool retVal;
        await CrossFirebaseAuth.Current.CreateUserAsync(email, password);

        if (CrossFirebaseAuth.Current.CurrentUser?.Uid != null && CrossFirebaseAuth.Current.CurrentUser.Uid.Length > 0)
        {
            await CrossFirebaseAuth.Current.CurrentUser.SendEmailVerificationAsync();
            await FirestoreManager.SetDocumentData(COLLECTION_USER_DATA,
                new Client_Firebase() { UserID = CrossFirebaseAuth.Current.CurrentUser.Uid, Email = email },
                DocumentID: CrossFirebaseAuth.Current.CurrentUser.Uid);
            retVal = true;
        }
        else
        {
            retVal = false;
        }

        await LogOutAsync();
        return retVal;
    }

    public static async Task<bool> ValidateCurrentSession()
    {
        try
        {
            if (CrossFirebaseAuth.Current.CurrentUser != null)
            {
                Client user = await GetClientDataAsync(CrossFirebaseAuth.Current.CurrentUser.Uid);
                if(!user.UserDataRetrieved)
                {
                    await CrossFirebaseAuth.Current.SignOutAsync();
                    return false;
                }
                await SessionManager.StartSessionAsync(user);
                await UpdateCurrentUserFCMToken();
                return true;
            }
        }
        catch (Exception ex)
        {
            await UserInterface.DisplayPopUp_Regular("Unhandled Database Exception", ex.Message, "Ok");
            SessionManager.CloseSession();
        }
        return false;
    }

    public static async Task LogOutAsync(FirebaseLocation? lastLocation = null, bool skipLocationUpdate = false)
    {
        if (!skipLocationUpdate && lastLocation != null)
        {
            await UpdateClientLocationAsync(CrossFirebaseAuth.Current.CurrentUser.Uid, lastLocation);
        }
        await CrossFirebaseAuth.Current.SignOutAsync();
    }
    #endregion

    #region Gets
    public async static Task<Client> GetClientDataAsync(string userID)
    {
        Client_Firebase? clientFB = await FirestoreManager.GetDocumentData<Client_Firebase>(COLLECTION_USER_DATA, userID);

        Client user = clientFB != null ? new(clientFB, await clientFB.GetImageSource()) : new() { UserID = userID };

        return user;
    }

    public async static Task<Spot?> GetSpotDataAsync(string spotID)
    {
        Spot_Firebase? spotFB = await FirestoreManager.GetDocumentData<Spot_Firebase>(COLLECTION_SPOTS, spotID);

        Spot? spot = spotFB == null ? null : new(spotFB, await spotFB.GetImageSource());

        return spot;
    }
    #endregion

    #region Saves
    public static async Task<bool> SaveUserDataAsync(Client user, string profilePictureAddress = "")
    {
        await FirestoreManager.SetDocumentData(COLLECTION_USER_DATA, new Client_Firebase(user, profilePictureAddress), DocumentID: user.UserID);

        return true;
    }

    public static async Task<bool> SaveSpotDataAsync(Spot spot, ImageFile? imageFile = null)
    {
        if (spot.SpotID.Length > 0)
        {
            string profilePictureAddress = "";
            if (imageFile != null)
            {
                profilePictureAddress = await FirebaseStorageManager.SaveFile($"{COLLECTION_SPOTS}/{spot.SpotID}", "SpotPicture", imageFile);
            }
            await FirestoreManager.SetDocumentData(COLLECTION_SPOTS, new Spot_Firebase(spot, profilePictureAddress), DocumentID: spot.SpotID);
        }
        else
        {
            spot.SpotID = await FirestoreManager.SetDocumentData(COLLECTION_SPOTS, new Spot_Firebase(spot, ""));
            if (imageFile != null)
            {
                string tablePictureAddress = await FirebaseStorageManager.SaveFile($"{COLLECTION_SPOTS}/{spot.SpotID}", "SpotsPicture", imageFile);
                await FirestoreManager.UpdateSpecificData(COLLECTION_SPOTS, spot.SpotID, nameof(Spot_Firebase.ProfilePictureAddress), tablePictureAddress);
            }
        }

        return true;
    }

    public static async Task<bool> SaveTableDataAsync(Table table, ImageFile? imageFile = null)
    {
        if (table.TableID.Length > 0)
        {
            string profilePictureAddress = "";
            if (imageFile != null)
            {
                profilePictureAddress = await FirebaseStorageManager.SaveFile($"{COLLECTION_TABLES}/{table.TableID}", "TablePicture", imageFile);
            }
            await FirestoreManager.SetDocumentData(COLLECTION_TABLES, new Table_Firebase(table, profilePictureAddress), table.TableID);
        }
        else
        {
            string tableID = await FirestoreManager.SetDocumentData(COLLECTION_TABLES, new Table_Firebase(table, ""));
            if (imageFile != null)
            {
                string tablePictureAddress = await FirebaseStorageManager.SaveFile($"{COLLECTION_TABLES}/{table.TableID}", "TablePicture", imageFile);
                await FirestoreManager.UpdateSpecificData(COLLECTION_TABLES, tableID, nameof(Table_Firebase.TablePictureAddress), tablePictureAddress);
            }
        }

        return true;
    }

    public static async Task SaveNotificationData(Notification_TableInvite notification)
    {
        // Verify is a new Notification
        if(notification.NotificationID.Length == 0)
        {
            // Verify there are no other invitations to the same user and to the same table.
            List<INotification> notificationList = await FetchNotifications_Filtered(notification.OwnerID, notification.Type, notification.Metadata_TableID);
            if (notificationList.Count == 0)
            {
                await FirestoreManager.SetDocumentData(COLLECTION_NOTIFICATIONS, new Notification_Firebase(notification));
            }
        }
    }
    #endregion

    #region Deletes
    public static Task DeleteNotificationData(INotification notification)
    {
        return FirestoreManager.DeleteDocument(COLLECTION_NOTIFICATIONS, notification.NotificationID);
    }
    #endregion

    #region Updates
    public static Task UpdateClientLocationAsync(string userID, FirebaseLocation location)
    {
        return FirestoreManager.UpdateSpecificData(COLLECTION_USER_DATA, userID, nameof(Client_Firebase.LastLocation), location);
    }

    public static async Task UpdateCurrentUserFCMToken()
    {
        string FCMToken = await CloudMessaging.CloudMessagingManager.GetFCMTokenAsync();
        if (FCMToken.Length > 0
            && SessionManager.CurrentSession?.Client != null
            && SessionManager.CurrentSession.Client.FCMToken != FCMToken)
        {
            SessionManager.CurrentSession.Client.FCMToken = FCMToken;
            await UpdateClientFCMToken(SessionManager.CurrentSession.Client.UserID, FCMToken);
        }
    }

    public static Task UpdateClientFCMToken(string userID, string FCMToken)
    {
        return FirestoreManager.UpdateSpecificData(COLLECTION_USER_DATA, userID, nameof(Client_Firebase.FCMToken), FCMToken);
    }
    #endregion

    #region Fetches
    public static async Task<List<Client>> FetchClientsByID(List<string> clientIDs)
    {
        List<Client> clients = [];

        if (clientIDs.Count > 0)
        {
            Dictionary<string, object[]> arrayContainsAnyFilter = new Dictionary<string, object[]>() { { nameof(Client_Firebase.ClientID_ForSearch), clientIDs.ToArray() } };
            
            List<Client_Firebase> clients_Firebase = await FirestoreManager.QueryFiltered<Client_Firebase>(COLLECTION_USER_DATA, filters_ArrayContainsAny: arrayContainsAnyFilter);

            foreach (Client_Firebase client in clients_Firebase?? [])
            {
                clients.Add(new(client, await client.GetImageSource()));
            }
        }

        return clients;
    }

    public static async Task<List<Client>> FetchClients_Filtered(
        string[]? nameSearchTerms = null,
        string? currentUsrID_ToAvoid = null,
        string? order = null,
        Client? lastClient = null)
    {
        List<Client> retVal = [];

        const int maxItemsToFetch = 25;
        Dictionary<string, object[]>? arrayContainsAnyFilters = null;
        string? lastItemFetchedID = lastClient?.UserID;
        string? orderBy = order;

        if (nameSearchTerms != null)
        {
            arrayContainsAnyFilters ??= [];
            arrayContainsAnyFilters.Add(nameof(Client_Firebase.SearchTerms), nameSearchTerms);
        }

        List<Client_Firebase>? clients_Firebase = await FirestoreManager.QueryFiltered<Client_Firebase>(COLLECTION_USER_DATA,
            maxItems: maxItemsToFetch,
            filters_ArrayContainsAny: arrayContainsAnyFilters,
            lastItemQueriedID: lastItemFetchedID,
            filters_orderBy: orderBy);

        foreach (var clientFB in clients_Firebase?? [])
        {
            if (currentUsrID_ToAvoid == null || clientFB.UserID != currentUsrID_ToAvoid)
            {
                retVal.Add(new(clientFB, await clientFB.GetImageSource()));
            }
        }

        return retVal;
    }

    public static async Task<List<Client>> FetchClients_Filtered(
        string? currentUsrID_ToAvoid = null,
        FirebaseLocation? referenceLocation = null,
        int? searchAreaInKm = null,
        string? order = null,
        Client? lastClient = null)
    {
        List<Client> retVal = [];

        const int maxItemsToFetch = 25;
        Dictionary<string, object[]>? arrayContainsAnyFilters = null;
        string? lastItemFetchedID = lastClient?.UserID;
        string? orderBy = order;

        if (referenceLocation != null && searchAreaInKm != null)
        {
            arrayContainsAnyFilters ??= [];
            List<string> geohashesNearby = GenerateGeohashSearchGrid(referenceLocation, (int)searchAreaInKm);
            geohashesNearby.Add(referenceLocation.Geohash[0]);

            arrayContainsAnyFilters.Add($"{nameof(Client_Firebase.LastLocation)}.Geohash", geohashesNearby.ToArray());
        }

        List<Client_Firebase>? clients_Firebase = await FirestoreManager.QueryFiltered<Client_Firebase>(COLLECTION_USER_DATA,
            maxItems: maxItemsToFetch,
            filters_ArrayContainsAny: arrayContainsAnyFilters,
            lastItemQueriedID: lastItemFetchedID,
            filters_orderBy: orderBy);

        foreach (var clientFB in clients_Firebase ?? [])
        {
            if (currentUsrID_ToAvoid == null || clientFB.UserID != currentUsrID_ToAvoid)
            {
                retVal.Add(new(clientFB, await clientFB.GetImageSource()));
            }
        }

        return retVal;
    }

    public static async Task<List<SpotPraise>> FetchSpotPraises_FromFollowedClients(Client client, SpotPraise? lastPraise = null)
    {
        List<SpotPraise> spotPraises = [];
        // Show personal praises too
        List<string> praiseAuthorIDs = client.Followed;
        praiseAuthorIDs.Add(client.UserID);

        // Query Filters
        const int maxItemsToFetch = 25;
        Dictionary<string, object[]>? arrayContainsAnyFilters =  new() { { nameof(SpotPraise_Firebase.AuthorID_Array), praiseAuthorIDs.ToArray() } };
        string? lastItemFetchedID = lastPraise?.PraiseID ?? null;
        string orderBy = nameof(SpotPraise_Firebase.CreationDate);

        if (client.FollowedCount > 0)
        {
            List<SpotPraise_Firebase> praises_Firebase = await FirestoreManager.QueryFiltered<SpotPraise_Firebase>(COLLECTION_PRAISES,
                maxItems: maxItemsToFetch,
                filters_ArrayContainsAny: arrayContainsAnyFilters,
                lastItemQueriedID: lastItemFetchedID,
                filters_orderBy: orderBy);

            spotPraises = await SpotPraise.GetPraisesFromFirebaseObject(praises_Firebase);
        }

        return spotPraises;
    }

    public static async Task<List<SpotPraise>> FetchSpotPraises_Filtered(
        string? authorId = null,
        Client? author = null,
        string? spotId = null,
        Spot? spot = null,
        FirebaseLocation? referenceLocation = null,
        int? searchAreaInKm = null,
        string? order = null,
        SpotPraise? lastPraise = null)
    {
        List<SpotPraise> retVal = [];

        const int maxItemsToFetch = 5;
        Dictionary<string, object>? equalsToFilters = null;
        Dictionary<string, object[]>? arrayContainsAnyFilters = null;
        string? lastItemFetchedID = lastPraise == null ? null : lastPraise.PraiseID;
        // The '?.' seems to not be working properly.
        //string? lastItemFetchedID = lastPraise?.PraiseID;
        string? orderBy = order;

        if (author != null ^ authorId != null)
        {
            equalsToFilters ??= [];
            author ??= await GetClientDataAsync(authorId?? "");
            equalsToFilters.Add(nameof(SpotPraise_Firebase.AuthorID), author.UserID);
        }
        if (spot != null || spotId != null)
        {
            equalsToFilters ??= [];
            spot ??= await GetSpotDataAsync(spotId?? "");
            equalsToFilters.Add(nameof(SpotPraise_Firebase.SpotID), spot.SpotID);
        }
        if (referenceLocation != null && searchAreaInKm != null)
        {
            arrayContainsAnyFilters ??= [];
            List<string> geohashesNearby = GenerateGeohashSearchGrid(referenceLocation, (int)searchAreaInKm);
            geohashesNearby.Add(referenceLocation.Geohash[0]);

            arrayContainsAnyFilters.Add($"{nameof(SpotPraise_Firebase.SpotLocation)}.Geohash", geohashesNearby.ToArray());
        }

        List<SpotPraise_Firebase> praises_Firebase = await FirestoreManager.QueryFiltered<SpotPraise_Firebase>(COLLECTION_PRAISES, maxItemsToFetch,
            filters_EqualsTo: equalsToFilters,
            filters_ArrayContainsAny: arrayContainsAnyFilters,
            filters_orderBy: orderBy,
            lastItemQueriedID: lastItemFetchedID
        );

        retVal = await SpotPraise.GetPraisesFromFirebaseObject(praises_Firebase, author: author, spot: spot);

        return retVal;
    }

    public static async Task<List<Spot>> FetchSpots_Filtered(
        FirebaseLocation? referenceLocation = null,
        int? searchAreaInKm = null,
        string? order = null,
        Spot? lastSpot = null)
    {
        List<Spot> retVal = [];

        const int maxItemsToFetch = 25;
        Dictionary<string, object[]>? arrayContainsAnyFilters = null;
        string? lastItemFetchedID = lastSpot?.SpotID;
        string? orderBy = order;

        if (referenceLocation != null && searchAreaInKm != null)
        {
            arrayContainsAnyFilters ??= [];
            List<string> geohashesNearby = GenerateGeohashSearchGrid(referenceLocation, (int)searchAreaInKm);
            geohashesNearby.Add(referenceLocation.Geohash[0]);

            arrayContainsAnyFilters.Add($"{nameof(Spot_Firebase.Location)}.Geohash", geohashesNearby.ToArray());
        }
        
        List<Spot_Firebase> spots_Firebase = await FirestoreManager.QueryFiltered<Spot_Firebase>(COLLECTION_SPOTS,
            maxItems:maxItemsToFetch,
            filters_ArrayContainsAny: arrayContainsAnyFilters,
            lastItemQueriedID: lastItemFetchedID,
            filters_orderBy: orderBy);

        foreach (var spot_Firebase in spots_Firebase)
        {
            retVal.Add(new(spot_Firebase, await spot_Firebase.GetImageSource()));
        }

        return retVal ?? [];
    }

    public static async Task<List<Spot>> FetchSpots_Filtered(
        string[]? filterParams = null,
        string? order = null,
        Spot? lastSpot = null)
    {
        List<Spot> retVal = [];

        const int maxItemsToFetch = 25;
        Dictionary<string, object[]>? arrayContainsAnyFilters = null;
        string? lastItemFetchedID = lastSpot?.SpotID;
        string? orderBy = order;

        if (filterParams != null)
        {
            arrayContainsAnyFilters ??= [];
            arrayContainsAnyFilters.Add(nameof(Spot_Firebase.SearchTerms), filterParams);
        }

        List<Spot_Firebase> spots_Firebase = await FirestoreManager.QueryFiltered<Spot_Firebase>(COLLECTION_SPOTS,
            maxItems: maxItemsToFetch,
            filters_ArrayContainsAny: arrayContainsAnyFilters,
            lastItemQueriedID: lastItemFetchedID,
            filters_orderBy: orderBy);

        foreach (var spot_Firebase in spots_Firebase)
        {
            retVal.Add(new(spot_Firebase, await spot_Firebase.GetImageSource()));
        }

        return retVal ?? [];
    }

    public static async Task<List<INotification>> FetchNotifications_Filtered(
        string? ownerID = null,
        string? type = null,
        string? metadataToLookFor = null,
        INotification? lastNotification = default)
    {
        List<INotification> retVal = [];

        const int maxItemsToFetch = 25;
        Dictionary<string, object>? equalsToFilters = null;
        Dictionary<string, object>? arrayContainsSingleFilters = null;
        string? lastItemFetchedID = lastNotification?.NotificationID;
        string? orderBy = nameof(Notification_Firebase.CreationDate);

        if (ownerID != null)
        {
            equalsToFilters ??= [];
            equalsToFilters.Add(nameof(Notification_Firebase.OwnerID), ownerID);
        }
        if (type != null)
        {
            equalsToFilters ??= [];
            equalsToFilters.Add(nameof(Notification_Firebase.Type), type);
        }
        if (metadataToLookFor != null)
        {
            arrayContainsSingleFilters ??= [];
            arrayContainsSingleFilters.Add(nameof(Notification_Firebase.Metadata), metadataToLookFor);
        }

        List<Notification_Firebase> notifications_Firebase = await FirestoreManager.QueryFiltered<Notification_Firebase>(COLLECTION_NOTIFICATIONS,
            maxItems: maxItemsToFetch,
            filters_EqualsTo: equalsToFilters,
            filters_ArrayContainsSingle: arrayContainsSingleFilters,
            lastItemQueriedID: lastItemFetchedID,
            filters_orderBy: orderBy);

        foreach (var notificationFB in notifications_Firebase)
        {
            switch(notificationFB.Type)
            {
                case Notification.NOTIFICATION_TYPE_TABLEINVITE:
                {
                    retVal.Add(new Notification_TableInvite(notificationFB));
                    break;
                }
            }
        }

        return retVal;
    }

    // TODO: Consider moving this method to the Discovery Page logic.
    public static async Task<List<object>> FetchDiscoveryPageItems(DiscoveryPageFilters filters, FirebaseLocation currentLocation, FirebaseLocation selectedLocation, object? lastItem = null)
    {
        List<object> retVal = [];

        switch (filters.Subject)
        {
            case DiscoveryPageFilters.FILTER_SUBJECT.CLIENTS:
                {
                    string orderCode = nameof(Client.FollowersCount);
                    FirebaseLocation location = filters.Location == DiscoveryPageFilters.FILTER_LOCATION.CURRENT
                        ? currentLocation : selectedLocation;
                    List<Client> clients = await FetchClients_Filtered(referenceLocation: location, searchAreaInKm: (int)filters.Area, order: orderCode, lastClient: lastItem != null ? (Client)lastItem : null);
                    foreach (Client client in clients)
                    {
                        retVal.Add(client);
                    }
                    break;
                }
            case DiscoveryPageFilters.FILTER_SUBJECT.SPOTS:
                {
                    string orderCode = nameof(Spot.PraiseCount);
                    FirebaseLocation location = filters.Location == DiscoveryPageFilters.FILTER_LOCATION.CURRENT
                        ? currentLocation : selectedLocation;
                    List<Spot> spots = await FetchSpots_Filtered(referenceLocation: location, searchAreaInKm: (int)filters.Area, order: orderCode, lastSpot: lastItem != null ? (Spot)lastItem : null);
                    foreach (Spot spot in spots)
                    {
                        retVal.Add(spot);
                    }
                    break;
                }
            case DiscoveryPageFilters.FILTER_SUBJECT.SPOT_PRAISES:
                {
                    string orderCode = filters.Order switch
                    {
                        DiscoveryPageFilters.FILTER_ORDER.POPULARITY => nameof(SpotPraise_Firebase.CreationDate),
                        _ => nameof(SpotPraise_Firebase.LikesCount)
                    };
                    FirebaseLocation location = filters.Location == DiscoveryPageFilters.FILTER_LOCATION.CURRENT
                        ? currentLocation : selectedLocation;
                    List<SpotPraise> praises = await FetchSpotPraises_Filtered(referenceLocation: location, searchAreaInKm: (int)filters.Area, order: orderCode, lastPraise: lastItem != null ? (SpotPraise)lastItem : null);
                    foreach (SpotPraise praise in praises)
                    {
                        retVal.Add(praise);
                    }
                    break;
                }
            default:
                break;
        }

        return retVal;
    }

    public static async Task<List<Table>> FetchTables_Filtered(string? tableMemberID = null, Table? lastItem = null)
    {
        List<Table> retVal = [];

        const int maxItemsToFetch = 5;
        Dictionary<string, object>? arrayContainsSingleFilters = null;
        string? lastItemFetchedID = lastItem?.TableID;
        //string? orderBy = order;

        if (tableMemberID != null)
        {
            arrayContainsSingleFilters ??= [];
            arrayContainsSingleFilters.Add(nameof(Table_Firebase.TableMembers), tableMemberID);
        }
        List<Table_Firebase> tables_Firebase = await FirestoreManager.QueryFiltered<Table_Firebase>(COLLECTION_TABLES,
            maxItems: maxItemsToFetch,
            filters_ArrayContainsSingle: arrayContainsSingleFilters,
            lastItemQueriedID: lastItemFetchedID);

        foreach (var table_Firebase in tables_Firebase)
        {
            retVal.Add(new(table_Firebase, await table_Firebase.GetImageSource()));
        }

        return retVal;
    }
    #endregion

    #region Transactions
    public static async Task<bool> Transaction_SaveSpotPraiseData(SpotPraise praise, ImageFile? imageFile = null)
    {
        SpotPraise_Firebase praise_Firebase = new(praise);
        if (imageFile != null)
        {
            praise_Firebase.AttachedPictureAddress = await SaveFile($"Users/{praise_Firebase.AuthorID}/PraiseAttachments", praise_Firebase.PraiseID, imageFile);
        }

        // Get document reference
        IDocumentReference praiseDocRef = CrossFirebaseFirestore.Current.GetCollection(COLLECTION_PRAISES).GetDocument(praise_Firebase.PraiseID);
        IDocumentReference spotDocRef = CrossFirebaseFirestore.Current.GetCollection(COLLECTION_SPOTS).GetDocument(praise_Firebase.SpotID);

        return await CrossFirebaseFirestore.Current.RunTransactionAsync(transaction =>
        {
            IDocumentSnapshot<Spot_Firebase> existingSpot = transaction.GetDocument<Spot_Firebase>(spotDocRef);

            // Update Praise Data
            transaction.UpdateData(praiseDocRef, (nameof(praise_Firebase.Comment), praise_Firebase.Comment));
            // Only update the picture address if its an existing praise, else the immage is added in the AddDocumentAsync call.
            if (imageFile != null && praise_Firebase.PraiseID.Length > 0)
            {
                transaction.UpdateData(praiseDocRef, (nameof(praise_Firebase.AttachedPictureAddress), praise_Firebase.AttachedPictureAddress));
            }

            // Update Spot Praise Count
            if (existingSpot?.Data == null)
            {
                // Magic number 1 = Initial Praise Count
                transaction.SetData(spotDocRef, new Spot_Firebase(spotDocRef.Id, praise.SpotFullName, praise.SpotLocation, praise.SpotProfilePictureAddress ?? "", 1));
            }
            else if (praise_Firebase.PraiseID.Length == 0)
            {
                // If its a new praise, we increase the praise count
                transaction.UpdateData(spotDocRef, (nameof(Spot_Firebase.PraiseCount), existingSpot.Data.PraiseCount + 1));
            }

            return true;
        });
    }
    public static async Task<bool> Transaction_DeleteTableAsync(string tableID)
    {
        bool retVal = false;
        IDocumentReference tableDocument = CrossFirebaseFirestore.Current.GetCollection(COLLECTION_TABLES).GetDocument(tableID);

        await CrossFirebaseFirestore.Current.RunTransactionAsync(async transaction => {
            IDocumentSnapshot<Table_Firebase> table = transaction.GetDocument<Table_Firebase>(tableDocument);

            if (table?.Data != null)
            {
                if (table.Data.TablePictureAddress.Length > 0)
                {
                    await FirebaseStorageManager.DeleteFile(table.Data.TablePictureAddress);
                }
                transaction.DeleteDocument(tableDocument);
                retVal = true;
            }
        });

        return retVal;
    }

    public static async Task<bool> Transaction_RemoveUserFromTable(string clientID, string tableID)
    {
        bool retVal = true;
        IDocumentReference tableDocument = CrossFirebaseFirestore.Current.GetCollection(COLLECTION_TABLES).GetDocument(tableID);

        bool isTableEmpty = await CrossFirebaseFirestore.Current.RunTransactionAsync<bool>(transaction => {
            bool isTableEmpty = false;
            IDocumentSnapshot<Table_Firebase> table = transaction.GetDocument<Table_Firebase>(tableDocument);

            if (table?.Data != null)
            {
                if(table.Data.TableMembers.Contains(clientID))
                {
                    IList<string> newTableMembers = table.Data.TableMembers;
                    newTableMembers.Remove(clientID);
                    if (newTableMembers.Count == 0)
                    {
                        isTableEmpty = true;
                    }
                    else
                    {
                        transaction.UpdateData(tableDocument, (nameof(Table_Firebase.TableMembers), newTableMembers));
                    }
                }
            }

            return isTableEmpty;
        });

        if (isTableEmpty)
        {
            await Transaction_DeleteTableAsync(tableID);
        }

        return retVal;
    }

    public static async Task<bool?> Transaction_UpdateLikeOnSpotPraise(string clientID, SpotPraise praise)
    {
        bool? retVal = null;
        IDocumentReference praiseDocument = CrossFirebaseFirestore.Current.GetCollection(COLLECTION_PRAISES).GetDocument(praise.PraiseID);
        IDocumentReference authorDocument = CrossFirebaseFirestore.Current.GetCollection(COLLECTION_USER_DATA).GetDocument(praise.AuthorID);

        retVal = await CrossFirebaseFirestore.Current.RunTransactionAsync<bool?>(transaction => {
            IDocumentSnapshot<SpotPraise_Firebase> praise = transaction.GetDocument<SpotPraise_Firebase>(praiseDocument);
            IDocumentSnapshot<Client_Firebase> author = transaction.GetDocument<Client_Firebase>(authorDocument);
            
            if (praise?.Data != null && author?.Data != null)
            {
                bool alreadyLiked = praise.Data.Likes.Contains(clientID);

                var updatedAuthorCount = author.Data.LikesCount + (alreadyLiked ? -1 : 1);
                var updatedPraiseCount = praise.Data.LikesCount + (alreadyLiked ? -1 : 1);
                var updatedPraiseList = praise.Data.Likes;
                if (alreadyLiked)
                {
                    updatedPraiseList.Remove(clientID);
                }
                else
                {
                    updatedPraiseList.Add(clientID);
                }

                transaction.UpdateData(authorDocument, (nameof(Client_Firebase.LikesCount), updatedAuthorCount));
                transaction.UpdateData(praiseDocument, (nameof(SpotPraise_Firebase.LikesCount), updatedPraiseCount));
                transaction.UpdateData(praiseDocument, (nameof(SpotPraise_Firebase.Likes), updatedPraiseList));

                // We return the contrary to the AlreadyLiked state to represent if the end result is LIKED or not.
                return !alreadyLiked;
            }

            return null;
        });

        return retVal;
    }

    public static async Task<bool> Transaction_UpdateClientFollowedList(string followerID, string followedID, bool follow)
    {
        bool retVal = false;

        IDocumentReference followedDocument = CrossFirebaseFirestore.Current.GetCollection(COLLECTION_USER_DATA).GetDocument(followedID);
        IDocumentReference followerDocument = CrossFirebaseFirestore.Current.GetCollection(COLLECTION_USER_DATA).GetDocument(followerID);

        await CrossFirebaseFirestore.Current.RunTransactionAsync(transaction => {
            bool retVal = false;

            IDocumentSnapshot<Client_Firebase> followed = transaction.GetDocument<Client_Firebase>(followedDocument);
            IDocumentSnapshot<Client_Firebase> follower = transaction.GetDocument<Client_Firebase>(followerDocument);

            if (followed?.Data != null && follower?.Data != null)
            {
                bool followedChangesRequired = follow ? !followed.Data.Followers.Contains(followerID) : followed.Data.Followers.Contains(followerID);
                bool followerChangesRequired = follow ? !follower.Data.Followers.Contains(followedID) : follower.Data.Followers.Contains(followedID);
                List<string> CachedFollowers = [.. followed.Data.Followers];
                List<string> CachedFollowed = [.. follower.Data.Followers];
                if (follow)
                {
                    if (followedChangesRequired) { CachedFollowers.Add(followerID); }
                    if (followerChangesRequired) { CachedFollowed.Add(followedID); }
                }
                else if (!follow)
                {
                    if (followedChangesRequired) { CachedFollowers.Remove(followerID); }
                    if (followerChangesRequired) { CachedFollowed.Remove(followedID); }
                }
                transaction.UpdateData(followedDocument, (nameof(Client_Firebase.Followers), CachedFollowers));
                transaction.UpdateData(followedDocument, (nameof(Client_Firebase.Followed), CachedFollowed));

                retVal = true;
            }

            return retVal;
        });

        return retVal;
    }

    public static async Task Transaction_AddTableMember(string newMemberID, string tableID)
    {
        IDocumentReference tableDocument = CrossFirebaseFirestore.Current.GetCollection(COLLECTION_TABLES).GetDocument(tableID);

        await CrossFirebaseFirestore.Current.RunTransactionAsync(transaction => {
            bool retVal = false;

            IDocumentSnapshot<Table_Firebase> table = transaction.GetDocument<Table_Firebase>(tableDocument);

            if (table?.Data != null)
            {
                IList<string> membersList = table.Data.TableMembers;
                if(!membersList.Contains(newMemberID))
                {
                    membersList.Add(newMemberID);
                }
                
                transaction.UpdateData(tableDocument, (nameof(Table_Firebase.TableMembers), membersList));

                retVal = true;
            }

            return retVal;
        });
    }
    #endregion

    #region Utilities
    private static List<string> GenerateGeohashSearchGrid(FirebaseLocation centerPoint, int searchAreaMagnitude)
    {
        List<string> retVal = [];

        string lastGeohashPivot = centerPoint.Geohash[0];

        for(int step = 0; step < searchAreaMagnitude; step++)
        {
            // Start at North neighbor
            string currentGeohashPivot = LocationManager.Encoder.GetNeighbor(lastGeohashPivot, Geohash.Direction.North);
            lastGeohashPivot = currentGeohashPivot;
            retVal.Add(currentGeohashPivot);
            // Get all neighbors between North and West
            for(int neighborIndex = 0; neighborIndex <= step; neighborIndex++)
            {
                currentGeohashPivot = LocationManager.Encoder.GetNeighbor(currentGeohashPivot, Geohash.Direction.West);
                retVal.Add(currentGeohashPivot);
            }
            for (int neighborIndex = 0; neighborIndex <= step; neighborIndex++)
            {
                currentGeohashPivot = LocationManager.Encoder.GetNeighbor(currentGeohashPivot, Geohash.Direction.South);
                retVal.Add(currentGeohashPivot);
            }
            // Get all neighbors between North and South
            for (int neighborIndex = 0; neighborIndex <= step; neighborIndex++)
            {
                currentGeohashPivot = LocationManager.Encoder.GetNeighbor(currentGeohashPivot, Geohash.Direction.South);
                retVal.Add(currentGeohashPivot);
            }
            for (int neighborIndex = 0; neighborIndex <= step; neighborIndex++)
            {
                currentGeohashPivot = LocationManager.Encoder.GetNeighbor(currentGeohashPivot, Geohash.Direction.East);
                retVal.Add(currentGeohashPivot);
            }
            // Get all neighbors between South and East
            for (int neighborIndex = 0; neighborIndex <= step; neighborIndex++)
            {
                currentGeohashPivot = LocationManager.Encoder.GetNeighbor(currentGeohashPivot, Geohash.Direction.East);
                retVal.Add(currentGeohashPivot);
            }
            for (int neighborIndex = 0; neighborIndex <= step; neighborIndex++)
            {
                currentGeohashPivot = LocationManager.Encoder.GetNeighbor(currentGeohashPivot, Geohash.Direction.North);
                retVal.Add(currentGeohashPivot);
            }
            // Get all neighbors between East and North
            for (int neighborIndex = 0; neighborIndex <= step; neighborIndex++)
            {
                currentGeohashPivot = LocationManager.Encoder.GetNeighbor(currentGeohashPivot, Geohash.Direction.North);
                retVal.Add(currentGeohashPivot);
            }
            for (int neighborIndex = 0; neighborIndex < step; neighborIndex++) // Here we use '<' isntead of '<=' to prevent adding North neighbor again
            {
                currentGeohashPivot = LocationManager.Encoder.GetNeighbor(currentGeohashPivot, Geohash.Direction.West);
                retVal.Add(currentGeohashPivot);
            }
        }

        return retVal;
    }
    #endregion
}
