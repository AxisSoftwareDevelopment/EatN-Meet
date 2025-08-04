using eatMeet.Models;
using eatMeet.Database;

namespace eatMeet;

public partial class CP_SpotPraise : ContentPage
{
    private readonly SpotPraise _SpotPraise;
	public CP_SpotPraise(SpotPraise praise)
	{
        _SpotPraise = praise;
        BindingContext = _SpotPraise;

        DisplayInfo displayInfo = DeviceDisplay.MainDisplayInfo;
        double profilePictureDimensions = displayInfo.Height * 0.065;

        InitializeComponent();

        _FrameSpotPicture.HeightRequest = profilePictureDimensions;
        _FrameSpotPicture.WidthRequest = profilePictureDimensions;
		
        _FrameAuthorPicture.HeightRequest = profilePictureDimensions / 4;
        _FrameAuthorPicture.WidthRequest = profilePictureDimensions / 4;

        _imgAttachmentImage.MaximumHeightRequest = profilePictureDimensions * 2;

        _btnEdit.Clicked += _btnEdit_Clicked;
        _imgAuthorPicture.Clicked += _imgAuthorPicture_Clicked;
        _imgSpotImage.Clicked += _imgSpotImage_Clicked;

        if (praise.AuthorID == SessionManager.CurrentSession?.Client?.UserID)
        {
            _btnEdit.IsVisible = true;
        }
        else
        {
            _btnEdit.IsVisible = false;
        }

    }

    private async void _imgSpotImage_Clicked(object? sender, EventArgs e)
    {
        LockView();
        Spot spot = await DatabaseManager.GetSpotDataAsync(_SpotPraise.SpotID);
        await Navigation.PushAsync(new CP_SpotView(spot));
        UnlockView();
    }

    private async void _imgAuthorPicture_Clicked(object? sender, EventArgs e)
    {
        LockView();
        Client client = await DatabaseManager.GetClientDataAsync(_SpotPraise.AuthorID);
        await client.OpenClientView(FP_MainShell.MainNavigation);
        UnlockView();
    }

    private void _btnEdit_Clicked(object? sender, EventArgs e)
    {
        Navigation.PushAsync(new CP_UpdateSpotPraise(_SpotPraise));
    }

    private void LockView()
    {
        _btnEdit.IsEnabled = false;
        _imgAuthorPicture.IsEnabled = false;
        _imgSpotImage.IsEnabled = false;
    }

    private void UnlockView()
    {
        _btnEdit.IsEnabled = true;
        _imgAuthorPicture.IsEnabled = true;
        _imgSpotImage.IsEnabled = true;
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