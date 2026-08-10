using eatMeet.Models;

namespace eatMeet.DiscoveryPage;

public partial class CV_DiscoveryFeedDataTemplate : ContentView
{
	public CV_DiscoveryFeedDataTemplate()
	{
        BindingContextChanged += CV_SearchBarDataTemplate_BindingContextChanged;

        InitializeComponent();
	}

    private void CV_SearchBarDataTemplate_BindingContextChanged(object? sender, EventArgs e)
    {
        SetBinginds(BindingContext);
    }

    private void SetBinginds(object? Item)
    {
        if (Item?.GetType() == typeof(Client))
        {
            lblMainName.SetBinding(Label.TextProperty, nameof(Client.FullName));
            lblSecondaryName.SetBinding(Label.TextProperty, nameof(Client.Email));
            lblDetail.SetBinding(Label.TextProperty, nameof(Client.Description));
            imgMainImage.SetBinding(Image.SourceProperty, nameof(Client.ProfilePictureSource));
            HideLikeButton();
        }
        else if (Item?.GetType() == typeof(Spot))
        {
            lblMainName.SetBinding(Label.TextProperty, nameof(Spot.Name));
            lblSecondaryName.SetBinding(Label.TextProperty, nameof(Spot.PraiseCount));
            lblDetail.SetBinding(Label.TextProperty, $"{nameof(Spot.Location)}.{nameof(Spot.Location.Address)}");
            imgMainImage.SetBinding(Image.SourceProperty, nameof(Spot.ProfilePictureSource));
            HideLikeButton();
        }
        else if (Item?.GetType() == typeof(SpotPraise))
        {
            lblMainName.SetBinding(Label.TextProperty, nameof(SpotPraise.SpotFullName));
            lblSecondaryName.SetBinding(Label.TextProperty, nameof(SpotPraise.AuthorFullName));
            lblDetail.SetBinding(Label.TextProperty, nameof(SpotPraise.Comment));
            imgMainImage.SetBinding(Image.SourceProperty, nameof(SpotPraise.SpotProfilePicture));
            imgSecondaryImage.SetBinding(Image.SourceProperty, nameof(SpotPraise.AuthorProfilePicture));
            ShowLikeButton();
        }
        else
        {
            HideLikeButton();
        }
    }

    private void HideLikeButton()
    {
        imgLikeButton.IsVisible = false;
        imgLikeButton.RemoveBinding(Image.SourceProperty);
    }

    private void ShowLikeButton()
    {
        imgLikeButton.IsVisible = true;
        imgLikeButton.SetBinding(Image.SourceProperty, nameof(SpotPraise.LikeSource));
    }

    private async void LikeButtonClicked(object? sender, EventArgs e)
    {
        if (sender is not ImageButton likeButton || likeButton.BindingContext is not SpotPraise praise)
        {
            return;
        }

        likeButton.IsEnabled = false;
        if (SessionManager.CurrentSession?.Client != null)
        {
            bool? likedState = await praise.LikeSwitch(SessionManager.CurrentSession.Client.UserID);

            if (likedState != null)
            {
                if ((bool)likedState)
                {
                    praise.Likes.Add(SessionManager.CurrentSession.Client.UserID);
                    praise.LikesCount++;
                }
                else
                {
                    praise.Likes.Remove(SessionManager.CurrentSession.Client.UserID);
                    praise.LikesCount--;
                }
            }
        }
        likeButton.IsEnabled = true;
    }
}