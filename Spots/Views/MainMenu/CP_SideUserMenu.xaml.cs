using eatMeet.Database;
using eatMeet.Models;
using eatMeet.Utilities;

namespace eatMeet;

public partial class CP_SideUserMenu : ContentPage
{
    private readonly FeedContext<Table> CurrentFeedContext = new();
    public CP_SideUserMenu()
    {
        DisplayInfo displayInfo = DeviceDisplay.MainDisplayInfo;
        double profilePictureDimensions = displayInfo.Height * 0.065;

        InitializeComponent();
        BindingContext = SessionManager.CurrentSession?.Client;

        _FrameProfilePicture.WidthRequest = profilePictureDimensions;
        _FrameProfilePicture.HeightRequest = profilePictureDimensions;

        // Tables Collection view
        _colTables.BindingContext = CurrentFeedContext;
        _refreshView.Command = new Command(async () =>
        {
            await RefreshTablesFeed();
            _refreshView.IsRefreshing = false;
        });
        _colTables.RemainingItemsThreshold = 1;
        _colTables.RemainingItemsThresholdReached += OnItemThresholdReached;
        _colTables.SelectionChanged += _colTables_SelectionChanged;

        Task.Run(RefreshTablesFeed);

        FP_MainShell.FlyoutPresentedChanged += FP_MainShell_FlyoutPresentedChanged;
    }

    private async void FP_MainShell_FlyoutPresentedChanged()
    {
        await RefreshTablesFeed();
    }

    public async Task RefreshTablesFeed()
    {
        CurrentFeedContext.RefreshFeed(await FetchTables());
    }

    private void AddTableOnClicked(object sender, EventArgs e)
    {
        FP_MainShell.SetIsPresented(false);
        FP_MainShell.MainNavigation?.PushAsync(new CP_UpdateTable());
    }

    private void ProfilePictureOrMyProfileOnClicked(object sender, EventArgs e)
    {
        FP_MainShell.SetIsPresented(false);
        SessionManager.CurrentSession?.Client?.OpenClientView(FP_MainShell.MainNavigation);
    }

    private void PreferencesOnClicked(object sender, EventArgs e)
    {
        FP_MainShell.SetIsPresented(false);
        FP_MainShell.MainNavigation?.PushAsync(new CP_AppPreferences());
    }

    private async void LogOutOnClickedAsync(object sender, EventArgs e)
    {
        if (await UserInterface.DisplayPopPup_Choice("Log Out", "Are you sure?", "Yes", "Cancel"))
            SessionManager.CloseSession(shouldUpdateMainPage: true);
    }

    private async void OnItemThresholdReached(object? sender, EventArgs e)
    {
        CurrentFeedContext.AddElements(await FetchTables(CurrentFeedContext.LastItemFetched));
    }

    private async Task<List<Table>> FetchTables(Table? lastItemFetched = null)
    {
        List<Table> retVal = [];
        string userID = ((Client)BindingContext).UserID;

        if (userID.Length > 0)
        {
            retVal = await DatabaseManager.FetchTables_Filtered(tableMemberID: userID, lastItem: lastItemFetched);
        }

        return retVal;
    }

    private void _colTables_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count > 0)
        {
            FP_MainShell.MainNavigation?.PushAsync(new CP_TableView((Table)e.CurrentSelection[0]));
            FP_MainShell.SetIsPresented(false);
            _colTables.SelectedItem = null;
        }
    }
}