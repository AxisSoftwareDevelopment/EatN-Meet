using eatMeet.Models;
using eatMeet.Database;

namespace eatMeet;

public partial class CP_UserProfile : ContentPage
{
	private readonly Client user;
    private readonly FeedContext<SpotPraise> ClientPraisesContext = new();
    public CP_UserProfile(Client _user, bool following = false)
	{
		user = _user;

        DisplayInfo displayInfo = DeviceDisplay.MainDisplayInfo;
        double profilePictureDimensions = displayInfo.Height * 0.065;

        InitializeComponent();
		BindingContext = user;
        _colClientPraises.BindingContext = ClientPraisesContext;

        _colClientPraises.RemainingItemsThreshold = 1;
        _colClientPraises.SelectionChanged += _colClientPraises_SelectionChanged;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await RefreshFeed();
            _colClientPraises.RemainingItemsThresholdReached += OnItemThresholdReached;
        });

        _FrameProfilePicture.HeightRequest = profilePictureDimensions;
		_FrameProfilePicture.WidthRequest = profilePictureDimensions;

		if (user.UserID != SessionManager.CurrentSession?.Client?.UserID)
		{
			_btnEdit.IsVisible = false;
            _stackFollowingZone.IsVisible = false;
            _btnInviteToTable.IsVisible = true;
            if (following)
            {
                _btnFollow.IsVisible = false;
                _btnUnfollow.IsVisible = true;
            }
            else
            {
                _btnFollow.IsVisible = true;
                _btnUnfollow.IsVisible = false;
            }
        }
		else
		{
            _btnInviteToTable.IsVisible = false;
            _btnEdit.IsVisible = true;
            _btnFollow.IsVisible = false;
			_btnUnfollow.IsVisible = false;
		}
	}

    private async Task RefreshFeed()
    {
        ClientPraisesContext.RefreshFeed(await DatabaseManager.FetchSpotPraises_Filtered(author: user));
    }

    private async void OnItemThresholdReached(object? sender, EventArgs e)
    {
        ClientPraisesContext.AddElements(await DatabaseManager.FetchSpotPraises_Filtered(author: user, lastPraise: ClientPraisesContext.LastItemFetched));
    }

    private void _colClientPraises_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count > 0)
        {
            Navigation.PushAsync(new CP_SpotPraise((SpotPraise)e.CurrentSelection[0]));
            _colClientPraises.SelectedItem = null;
        }
    }

    private void EditPersonalInformation_OnClicked(object sender, EventArgs e)
	{
		Navigation.PushAsync(new CP_UpdateUserInformation(user));
	}

    private async void FollowedClientsView(object? sender, EventArgs e)
    {
        List<Client> followedClients = await DatabaseManager.FetchClientsByID(user.Followed);
        await Navigation.PushAsync(new CP_FollowedClientsView(followedClients));
    }

    private async void FollowClient_OnClicked(object sender, EventArgs e)
    {
        string followerID = SessionManager.CurrentSession?.Client?.UserID ?? "";
        string followedID = user.UserID;
        if (followerID.Length > 0)
        {
            if (await DatabaseManager.Transaction_UpdateClientFollowedList(followerID, followedID, true))
            {
                _btnFollow.IsVisible = false;
                _btnUnfollow.IsVisible = true;
            }
        }
    }

    private async void UnfollowClient_OnClicked(object sender, EventArgs e)
    {
        string followerID = SessionManager.CurrentSession?.Client?.UserID ?? "";
        string followedID = user.UserID;
        if (followerID.Length > 0)
        {
            if (await DatabaseManager.Transaction_UpdateClientFollowedList(followerID, followedID, false))
            {
                _btnFollow.IsVisible = true;
                _btnUnfollow.IsVisible = false;
            }
        }
    }

    private async void InviteToTable_OnClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CP_InviteUserToTable(user));
    }

    private async void LikeButtonClicked(object sender, EventArgs e)
    {
        ((ImageButton)sender).IsEnabled = false;
        if (SessionManager.CurrentSession?.Client != null)
        {
            bool? likedState = await ((SpotPraise)((ImageButton)sender).BindingContext).LikeSwitch(SessionManager.CurrentSession.Client.UserID);

            if (likedState != null)
            {
                if ((bool)likedState)
                {
                    ((SpotPraise)((ImageButton)sender).BindingContext).Likes.Add(SessionManager.CurrentSession.Client.UserID);
                    ((SpotPraise)((ImageButton)sender).BindingContext).LikesCount++;

                }
                else
                {
                    ((SpotPraise)((ImageButton)sender).BindingContext).Likes.Remove(SessionManager.CurrentSession.Client.UserID);
                    ((SpotPraise)((ImageButton)sender).BindingContext).LikesCount--;
                }
            }
        }
        ((ImageButton)sender).IsEnabled = true;
    }
}