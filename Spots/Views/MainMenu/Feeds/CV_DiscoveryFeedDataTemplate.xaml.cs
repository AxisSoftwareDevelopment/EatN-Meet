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
        }
        else if (Item?.GetType() == typeof(Spot))
        {
            lblMainName.SetBinding(Label.TextProperty, nameof(Spot.Name));
            //lblSecondaryName.SetBinding(Label.TextProperty, nameof(Spot.FullPhoneNumber));
            lblDetail.SetBinding(Label.TextProperty, $"{nameof(Spot.Location)}.{nameof(Spot.Location.Address)}");
            imgMainImage.SetBinding(Image.SourceProperty, nameof(Spot.ProfilePictureSource));
        }
        else if (Item?.GetType() == typeof(SpotPraise))
        {
            lblMainName.SetBinding(Label.TextProperty, nameof(SpotPraise.SpotFullName));
            lblSecondaryName.SetBinding(Label.TextProperty, nameof(SpotPraise.AuthorFullName));
            lblDetail.SetBinding(Label.TextProperty, nameof(SpotPraise.Comment));
            imgMainImage.SetBinding(Image.SourceProperty, nameof(SpotPraise.SpotProfilePicture));
            imgSecondaryImage.SetBinding(Image.SourceProperty, nameof(SpotPraise.AuthorProfilePicture));
        }
    }
}