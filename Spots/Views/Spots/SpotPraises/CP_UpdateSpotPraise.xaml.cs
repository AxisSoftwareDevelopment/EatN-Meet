using eatMeet.Models;
using eatMeet.ResourceManager;
using eatMeet.Database;
using eatMeet.Utilities;
using eatMeet.GooglePlacesService;

namespace eatMeet;

public partial class CP_UpdateSpotPraise : ContentPage
{
	private SpotPraise? MainSpotPraise;
    private readonly FeedContext<Spot> SearchBoxContext = new();
    private ImageFile? _AttachmentFile;
    private readonly DebouncedAction<string> DebouncedSearch;
    private bool _LoadingResults = false;

    public bool LoadingResults
    {
        get => _LoadingResults;
        set
        {
            _LoadingResults = value;
            _borderSpotSearch.IsVisible = _LoadingResults || SearchBoxContext.ItemSource.Count > 0;
            OnPropertyChanged(nameof(LoadingResults));
        }
    }
    public CP_UpdateSpotPraise() : this(null, null) { }
    public CP_UpdateSpotPraise(Spot spot) : this(null, spot) { }
    public CP_UpdateSpotPraise(SpotPraise spotPraise) : this(spotPraise, null) { }
    private CP_UpdateSpotPraise(SpotPraise? spotPraise = null, Spot? spot = null)
	{
        DisplayInfo displayInfo = DeviceDisplay.MainDisplayInfo;
        double profilePictureDimensions = displayInfo.Height * 0.065;

        InitializeComponent();

        _borderActLoadingIndicator.BindingContext = this;
        _actLoadingIndicator.BindingContext = this;
        _colSearchBarCollectionView.BindingContext = SearchBoxContext;
        _colSearchBarCollectionView.MaximumHeightRequest = profilePictureDimensions * 2;
        _colSearchBarCollectionView.SelectionChanged += _colSearchBarCollectionView_SelectionChanged;

        DebouncedSearch = new (async (searchText) =>
        {
            await RefreshSearchResults(searchText);

            // Show or hide the search results frame
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _colSearchBarCollectionView.IsVisible = LoadingResults || !string.IsNullOrEmpty(searchText) && SearchBoxContext.ItemSource.Count > 0;
            });
        });
        _entrySpotSearchBar.TextChanged += async (sender, e) =>
        {
            LoadingResults = true;
            await DebouncedSearch.Run(e.NewTextValue);
        };

        _FrameSpotPicture.HeightRequest = profilePictureDimensions;
        _FrameSpotPicture.WidthRequest = profilePictureDimensions;

        _btnSave.Clicked += _btnSave_Clicked;

        if (spot != null)
        {
            LoadSelectedSpot(spot);
            _entrySpotSearchBar.IsVisible = false;
            _colSearchBarCollectionView.IsVisible = false;
        }
        else
        {
            LoadSpotPraise(spotPraise);
        }
    }
    private void LoadSpotPraise(SpotPraise? praise)
    {
        MainSpotPraise = praise;

        if(MainSpotPraise != null)
        {
            _entrySpotSearchBar.IsVisible = false;
            _colSearchBarCollectionView.IsVisible = false;
            _lblBrand.Text = MainSpotPraise.SpotFullName;
            _editorDescription.Text = MainSpotPraise.Comment;
            _imgAttachmentImage.Source = MainSpotPraise.AttachedPicture;
            _SpotImage.Source = MainSpotPraise.SpotProfilePicture;
        }
    }

    private void _colSearchBarCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Spot selectedSpot = (Spot)e.CurrentSelection[0];
        MainSpotPraise = null;
        _entrySpotSearchBar.Text = "";
        _entrySpotSearchBar.Placeholder = selectedSpot.FullName;
        LoadSelectedSpot(selectedSpot);
    }

    private async Task RefreshSearchResults(string? searchInput)
    {
        try
        {
            if ((searchInput?.Length ?? 0) > 0)
            {
                Location? location = LocationManager.CurrentLocation ?? await LocationManager.GetUpdatedLocaionAsync();
                if (location != null)
                {
                    List<Spot> spotList = await GooglePlaces.GetAllRestaurants(searchInput?.ToUpper().Trim() ?? "", location, 1000, 5);
                    SearchBoxContext.RefreshFeed(spotList);
                }
            }
            else
            {
                SearchBoxContext.RefreshFeed([]);
            }
        }
        catch (Exception ex)
        {
            await UserInterface.DisplayPopUp_Regular("Unhandled Error", ex.Message, "OK");
        }

        LoadingResults = false;
    }

    public async void LoadImageOnClickAsync(object sender, EventArgs e)
    {
        ImageFile? image = await ImageManagement.PickImageFromInternalStorage();

        if (image != null)
        {
            _imgAttachmentImage.Source = ImageSource.FromStream(() => ImageManagement.ByteArrayToStream(image.Bytes ?? []));
            _AttachmentFile = image;
        }
    }

    private void LoadSelectedSpot(Spot spotSelected)
    {
        _lblBrand.Text = spotSelected.FullName;
        _SpotImage.Source = spotSelected.ProfilePictureSource;
        MainSpotPraise = new("", SessionManager.CurrentSession?.Client?.UserID ?? "", SessionManager.CurrentSession?.Client?.FullName ?? "", spotSelected.SpotID, spotSelected.Name, DateTimeOffset.Now, spotPictureAddress: spotSelected.ProfilePictureAddress);
    }

    private async void _btnSave_Clicked(object? sender, EventArgs e)
    {
        LockInputs();
        if(MainSpotPraise != null)
        {
            MainSpotPraise.Comment = _editorDescription.Text?.Trim() ?? "";

            if(await DatabaseManager.Transaction_SaveSpotPraiseData(MainSpotPraise, _AttachmentFile))
            {
                await Navigation.PopAsync();
            }
        }
        UnlockInputs();
    }

    private void LockInputs()
    {
        _btnLoadImage.IsEnabled = false;
        _btnSave.IsEnabled = false;
        _editorDescription.IsEnabled = false;
        _entrySpotSearchBar.IsEnabled = false;
    }

    private void UnlockInputs()
    {
        _btnLoadImage.IsEnabled = true;
        _btnSave.IsEnabled = true;
        _editorDescription.IsEnabled = true;
        _entrySpotSearchBar.IsEnabled = true;
    }
}