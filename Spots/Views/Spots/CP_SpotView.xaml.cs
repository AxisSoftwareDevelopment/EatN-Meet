using eatMeet.Database;
using eatMeet.Models;
using Firebase.Auth;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace eatMeet;

public partial class CP_SpotView : ContentPage
{
    private Spot CachedSpot;
    private readonly FeedContext<SpotPraise> ClientPraisesContext = new();
    public CP_SpotView(Spot spot)
	{
        CachedSpot = spot;

        DisplayInfo displayInfo = DeviceDisplay.MainDisplayInfo;
        double profilePictureDimensions = displayInfo.Height * 0.075;

        InitializeComponent();
        BindingContext = CachedSpot;

        _FrameProfilePicture.HeightRequest = profilePictureDimensions;
        _FrameProfilePicture.WidthRequest = profilePictureDimensions;

        Location spotLocation = new(spot.Location.Latitude, spot.Location.Longitude);
        _cvMiniMap.Pins.Clear();
        _cvMiniMap.MoveToRegion(new MapSpan(spotLocation, 0.01, 0.01));
        _cvMiniMap.Pins.Add(new Pin() { Label = spot.Location.Address, Location = spotLocation });
        _cvMiniMap.HeightRequest = profilePictureDimensions * 1.5;

        _colClientPraises.BindingContext = ClientPraisesContext;

        _colClientPraises.RemainingItemsThreshold = 1;
        _colClientPraises.SelectionChanged += _colClientPraises_SelectionChanged;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await RefreshFeed();
            _colClientPraises.RemainingItemsThresholdReached += OnItemThresholdReached;
        });

        _btnWriteReview.IsVisible = true;
    }

    private async Task RefreshFeed()
    {
        ClientPraisesContext.RefreshFeed(await DatabaseManager.FetchSpotPraises_Filtered(spot: CachedSpot));
    }

    private async void OnItemThresholdReached(object? sender, EventArgs e)
    {
        ClientPraisesContext.AddElements(await DatabaseManager.FetchSpotPraises_Filtered(spot: CachedSpot, lastPraise: ClientPraisesContext.LastItemFetched));
    }

    private void _colClientPraises_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count > 0)
        {
            Navigation.PushAsync(new CP_SpotPraise((SpotPraise)e.CurrentSelection[0]));
            _colClientPraises.SelectedItem = null;
        }
    }

    private void WriteSpotReview(object sender, EventArgs e)
    {
        Navigation.PushAsync(new CP_UpdateSpotPraise(CachedSpot));
    }

    private async void LikeButtonClicked(object sender, EventArgs e)
    {
        if (SessionManager.CurrentSession?.Client != null)
        {
            bool? likedState = await ((SpotPraise)((Button)sender).BindingContext).LikeSwitch(SessionManager.CurrentSession.Client.UserID);

            if (likedState != null)
            {
                if ((bool)likedState)
                {
                    ((SpotPraise)((Button)sender).BindingContext).Likes.Add(SessionManager.CurrentSession.Client.UserID);
                    ((SpotPraise)((Button)sender).BindingContext).LikesCount++;
                }
                else
                {
                    ((SpotPraise)((Button)sender).BindingContext).Likes.Remove(SessionManager.CurrentSession.Client.UserID);
                    ((SpotPraise)((Button)sender).BindingContext).LikesCount--;
                }
            }
        }
    }
}