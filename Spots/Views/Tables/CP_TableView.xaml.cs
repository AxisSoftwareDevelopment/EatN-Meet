using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

using eatMeet.Models;
using eatMeet.ResourceManager;
using eatMeet.Utilities;
using eatMeet.Database;
using eatMeet.Firestore;
using eatMeet.Notifications;

namespace eatMeet;

public partial class CP_TableView : ContentPage
{
    private const string STATE_COLOR_SITTING = "Green";
    private const string STATE_COLOR_AWAY = "#D42626";
    private string[] MemberStateLables = ["lbl_TableState_Sitting", "lbl_TableState_Away"];
    private string[] CurrentStateLables = ["lbl_CurrentTableState_Sitting", "lbl_CurrentTableState_Away"];
    private string[] InteractionLables = ["lbl_TableInteract_StanUp", "lbl_TableInteract_Sit"];
    private string[] ErrorLables = ["lbl_UnhandledError", "lbl_Ok"];
    private string[] DialogLables = ["lbl_AreYouSure", "txt_TableAbandonConfirmationMessage", "lbl_Abandon", "lbl_Cancel"];
    private Table CachedTable;
    private readonly FeedContext<TableMember> CurrentFeedContext = new();
    private bool _isInitializing = true;
    private bool _isUpdatingNotifications = false;

    public CP_TableView(Table table)
	{
		CachedTable = table;
        MemberStateLables = ResourceManagement.GetStringResources(Application.Current?.Resources, MemberStateLables);
        CurrentStateLables = ResourceManagement.GetStringResources(Application.Current?.Resources, CurrentStateLables);
        InteractionLables = ResourceManagement.GetStringResources(Application.Current?.Resources, InteractionLables);
        ErrorLables = ResourceManagement.GetStringResources(Application.Current?.Resources, ErrorLables);
        DialogLables = ResourceManagement.GetStringResources(Application.Current?.Resources, DialogLables);

        DisplayInfo displayInfo = DeviceDisplay.MainDisplayInfo;
        double profilePictureDimensions = displayInfo.Height * 0.065;

        InitializeComponent();

        BindingContext = CachedTable;

        _BorderTablePicture.HeightRequest = profilePictureDimensions;
        _BorderTablePicture.WidthRequest = profilePictureDimensions;

        Location spotLocation = new(table.Location.Latitude, table.Location.Longitude);
        _cvMiniMap.Pins.Clear();
        _cvMiniMap.MoveToRegion(new MapSpan(spotLocation, 0.01, 0.01));
        _cvMiniMap.Pins.Add(new Pin() { Label = table.Location.Address, Location = spotLocation });
        _cvMiniMap.HeightRequest = profilePictureDimensions;

        _entryAddress.IsVisible = true;

        // Members List Collection View
        _colMembers.BindingContext = CurrentFeedContext;
        _colMembers.SelectionChanged += _colMembers_SelectionChanged;
        Task.Run(() =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await RefreshMembersList();
            });
        });
        

        // Table state evaluation
        bool userIsSitting = CachedTable.SittingMembers.Contains(SessionManager.CurrentSession?.Client?.UserID ?? "NULL");
        _btnInteractWithTable.Text = userIsSitting ? InteractionLables[0] : InteractionLables[1];
        _lblCurrentState.Text = userIsSitting ? CurrentStateLables[0] : CurrentStateLables[1];
        _lblCurrentState.SetValue(BackgroundColorProperty, userIsSitting ? STATE_COLOR_SITTING : STATE_COLOR_AWAY);

        // Initialize notification toggle
        string? currentUserId = SessionManager.CurrentSession?.Client?.UserID;
        if (currentUserId != null)
        {
            bool receiveNotifications = GetUserNotificationPreference(currentUserId);
            _switchNotifications.IsToggled = receiveNotifications;
        }
        _switchNotifications.Toggled += _switchNotifications_Toggled;
        _isInitializing = false;

        _btnInteractWithTable.Clicked += _btnInteractWithTable_Clicked;
        _btnAbandonTable.Clicked += _btnAbandonTable_Clicked;
    }

    private async void _btnAbandonTable_Clicked(object? sender, EventArgs e)
    {
        if (await UserInterface.DisplayPopPup_Choice(DialogLables[0], DialogLables[1], DialogLables[2], DialogLables[3])
            && SessionManager.CurrentSession?.Client != null)
        {
            LockUI();
            try
            {
                await DatabaseManager.Transaction_RemoveUserFromTable(SessionManager.CurrentSession.Client.UserID, CachedTable.TableID);
                await Navigation.PopAsync();
            }
            finally
            {
                UnlockUI();
            }
        }
    }

    private async void _btnInteractWithTable_Clicked(object? sender, EventArgs e)
    {
        string? userID = SessionManager.CurrentSession?.Client?.UserID;
        if (userID != null)
        {
            LockUI();
            try
            {
                bool userIsSitting = CachedTable.SittingMembers.Contains(userID);
                if (userIsSitting)
                {
                    //CachedTable.SittingMembers.Remove(userID);
                    CachedTable.SittingMembers = (await DatabaseManager.Transaction_StandFromTableFromTable(userID, CachedTable.TableID)).ToList();

                    // Send notification to members who want notifications
                    await NotificationsManager.SendTableStandNotification(CachedTable, userID);
                }
                else
                {
                    //CachedTable.SittingMembers.Add(userID);
                    CachedTable.SittingMembers = (await DatabaseManager.Transaction_SitAtTableFromTable(userID, CachedTable.TableID)).ToList();

                    // Send notification to members who want notifications
                    await NotificationsManager.SendTableSitNotification(CachedTable, userID);
                }
                UpdateSittingStateUI(userID);

                await RefreshMembersList();
            }
            finally
            {
                UnlockUI();
            }
        }
    }

    private async Task RefreshMembersList()
    {
        CurrentFeedContext.RefreshFeed(await FetchMembers());
    }

    private async Task<List<TableMember>> FetchMembers()
    {
        List<TableMember> retVal = new List<TableMember>();

        if (SessionManager.CurrentSession?.Client != null)
        {
            try
            {
                List<Client> clients = await DatabaseManager.FetchClientsByID(CachedTable.TableMembers);
                foreach (Client client in clients)
                {
                    retVal.Add(new TableMember(client, CachedTable.SittingMembers.Contains(client.UserID), MemberStateLables));
                }
            }
            catch (Exception ex)
            {
                await UserInterface.DisplayPopUp_Regular(ErrorLables[0], ex.Message, ErrorLables[1]);
            }
        }

        return retVal;
    }

    private async void _colMembers_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count > 0)
        {
            await ((TableMember)e.CurrentSelection[0]).LaunchClientView();
            _colMembers.SelectedItem = null;
        }
    }

    private void UpdateSittingStateUI(string userID)
    {
        bool bIsSitting = CachedTable.SittingMembers.Contains(userID);
        _btnInteractWithTable.Text = bIsSitting ? InteractionLables[0] : InteractionLables[1];
        _lblCurrentState.Text = bIsSitting ? CurrentStateLables[0] : CurrentStateLables[1];
        _lblCurrentState.SetValue(BackgroundColorProperty, bIsSitting ? STATE_COLOR_SITTING : STATE_COLOR_AWAY);
    }

    private void LockUI()
    {
        _btnInteractWithTable.IsEnabled = false;
        _btnAbandonTable.IsEnabled = false;
        _switchNotifications.IsEnabled = false;
        _colMembers.IsEnabled = false;
    }

    private void UnlockUI()
    {
        _btnInteractWithTable.IsEnabled = true;
        _btnAbandonTable.IsEnabled = true;
        _switchNotifications.IsEnabled = true;
        _colMembers.IsEnabled = true;
    }

    private bool GetUserNotificationPreference(string userId)
    {
        if (CachedTable.MemberData.TryGetValue(userId, out var metadata))
        {
            return metadata.ReceiveTableNotifications;
        }
        return true; // Default to true if not set
    }

    private async void _switchNotifications_Toggled(object? sender, ToggledEventArgs e)
    {
        // Prevent saving during initialization
        if (_isInitializing)
            return;

        // Prevent concurrent updates
        if (_isUpdatingNotifications)
            return;

        string? userId = SessionManager.CurrentSession?.Client?.UserID;
        if (userId == null)
            return;

        _isUpdatingNotifications = true;
        LockUI();

        try
        {
            // Update local model
            if (!CachedTable.MemberData.ContainsKey(userId))
            {
                CachedTable.MemberData[userId] = new MemberMetadata();
            }
            CachedTable.MemberData[userId].ReceiveTableNotifications = e.Value;

            // Update in Firestore
            await FirestoreManager.UpdateSpecificData(
                "Tables",
                CachedTable.TableID,
                $"MemberData.{userId}.ReceiveTableNotifications",
                e.Value
            );
        }
        catch (Exception ex)
        {
            await UserInterface.DisplayPopUp_Regular(ErrorLables[0], ex.Message, ErrorLables[1]);
            // Revert toggle on error
            _isInitializing = true;
            _switchNotifications.IsToggled = !e.Value;
            _isInitializing = false;
        }
        finally
        {
            UnlockUI();
            _isUpdatingNotifications = false;
        }
    }

    private class TableMember
    {
        private const string STATE_COLOR_SITTING = "Green";
        private const string STATE_COLOR_AWAY = "Red";
        private Client CachedClient;
        public string Name { get; set; }
        public string State { get; set; }
        public string StateColor { get; set; }
        public ImageSource ProfilePictureSource { get; set; }

        public TableMember(Client client, bool sitting, string[] stateLbls)
        {
            CachedClient = client;
            Name = client.FullName;
            State = sitting ? stateLbls[0] : stateLbls[1];
            StateColor = sitting ? STATE_COLOR_SITTING : STATE_COLOR_AWAY;
            ProfilePictureSource = client.ProfilePictureSource;
        }

        public async Task LaunchClientView()
        {
            await CachedClient.OpenClientView(FP_MainShell.MainNavigation);
        }
    }
}