using eatMeet.Database;
using eatMeet.GooglePlacesService;
using eatMeet.Models;
using eatMeet.Utilities;
using Java.Lang;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace eatMeet;

public partial class CP_MapLocationSelector : ContentPage
{
    private Action<Location, string>? _setSelectedLocation;
    private string _selectedAddress;
    private Location _selectedLocation;
    private readonly FeedContext<Spot> SearchResultsListContext = new();
    private readonly DebouncedAction<string> DebouncedSearch;
    private bool _LoadingResults = false;

    public bool LoadingResults
    {
        get => _LoadingResults;
        set
        {
            _LoadingResults = value;
            _frameSearchResults.IsVisible = _LoadingResults || SearchResultsListContext.ItemSource.Count > 0;

            OnPropertyChanged(nameof(LoadingResults));
        }
    }
    public string SearchTextInput { get; set; } = "";

    public CP_MapLocationSelector(MapSpan? mapSpan, string address = "", Action<Location, string>? setSelectedLocation = null)
	{
		InitializeComponent();

        _setSelectedLocation = setSelectedLocation;

        _actLoadingIndicator.BindingContext = this;
        _colSearchBarCollectionView.BindingContext = SearchResultsListContext;
        _colSearchBarCollectionView.SelectionChanged += _colSearchBarCollectionView_SelectionChanged;

        DebouncedSearch = new(RefreshSearchResults);
        _entrySearchTerms.TextChanged += async (sender, e) =>
        {
            if (!LoadingResults)
            {
                LoadingResults = true;
            }
            await DebouncedSearch.Run(e.NewTextValue);
        };
        _lblSelectedAddress.Text = address;
        if(mapSpan == null)
        {
            return;
        }
        _selectedAddress = address?? "";
        _selectedLocation = mapSpan.Center;


        _cvMap.MoveToRegion(mapSpan);
        _cvMap.Pins.Clear();
        _cvMap.Pins.Add(new Pin()
        {
            Label = _selectedAddress,
            Location = _selectedLocation
        });

        _cvMap.MapClicked += _cvMap_MapClicked;

        _btnSelectLocation.Clicked += _btnSelectLocation_Clicked;
	}

    private void _btnSelectLocation_Clicked(object? sender, EventArgs e)
    {
        _setSelectedLocation?.Invoke(_selectedLocation, _selectedAddress);
        Navigation.PopAsync();
    }

    private async void _cvMap_MapClicked(object? sender, MapClickedEventArgs e)
    {
        string? address = await LocationManager.GetAddressFromLocation(e.Location);
        if (address != null)
        {
            _cvMap.Pins.Clear();
            _selectedAddress = address;
            _selectedLocation = e.Location;
            _lblSelectedAddress.Text = _selectedAddress;
            _cvMap.Pins.Add(new Pin()
            {
                Label = _selectedAddress,
                Location = _selectedLocation
            });
        }
    }

    private async void _colSearchBarCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count > 0)
        {
            //await Navigation.PushAsync(new CP_SpotView((Spot)e.CurrentSelection[0]));
            Spot spot = (Spot)e.CurrentSelection[0];
            _selectedAddress = spot.Location.Address;
            _selectedLocation = new Location(spot.Location.Latitude, spot.Location.Longitude);
            _lblSelectedAddress.Text = _selectedAddress;
            _cvMap.Pins.Clear();
            _cvMap.Pins.Add(new Pin() {
                Label = _selectedAddress,
                Location = _selectedLocation
            });
            _cvMap.MoveToRegion(new MapSpan(_selectedLocation, 0.01, 0.01));
            _entrySearchTerms.Text = "";
            _colSearchBarCollectionView.SelectedItem = null;
        }
    }

    private async Task RefreshSearchResults(string searchInput)
    {
       string[] inputs = searchInput != null ? [searchInput.ToUpper().Trim()] : [];
        if (inputs.Length > 0)
        {
            List<Spot> list = [];
            Location? location = LocationManager.CurrentLocation ?? await LocationManager.GetUpdatedLocaionAsync();
            if (location != null)
            {
                list = await GooglePlaces.GetAllRestaurants(searchInput?.ToUpper().Trim() ?? "", location, 1000, 5);
            }

            SearchResultsListContext.RefreshFeed(list);
        }
        else
        {
            SearchResultsListContext.RefreshFeed([]);
        }

        LoadingResults = false;
    }
}