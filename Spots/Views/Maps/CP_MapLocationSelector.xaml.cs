using eatMeet.Database;
using eatMeet.GooglePlacesService;
using eatMeet.Models;
using eatMeet.Utilities;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace eatMeet;

public partial class CP_MapLocationSelector : ContentPage
{
	private Func<MapSpan?> _MapSpanGetter;
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

    public CP_MapLocationSelector(Func<MapSpan?> mapSpanGetter, string address = "")
	{
		InitializeComponent();

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

        _MapSpanGetter = mapSpanGetter;
        _lblSelectedAddress.Text = address;

        MapSpan? mapSpan = _MapSpanGetter();
        if(mapSpan == null)
        {
            return;
        }

        _cvMap.MoveToRegion(mapSpan);
        _cvMap.Pins.Clear();
        _cvMap.Pins.Add(new Pin()
        {
            Label = address,
            Location = mapSpan.Center
        });

        _cvMap.MapClicked += _cvMap_MapClicked;
	}

    private async void _cvMap_MapClicked(object? sender, MapClickedEventArgs e)
    {
        string? address = await LocationManager.GetAddressFromLocation(e.Location);
        if (address != null)
        {
            _cvMap.Pins.Clear();
            _lblSelectedAddress.Text = address ?? "";
            _cvMap.Pins.Add(new Pin()
            {
                Label = address ?? "",
                Location = e.Location
            });
        }
    }

    private async void _colSearchBarCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count > 0)
        {
            //await Navigation.PushAsync(new CP_SpotView((Spot)e.CurrentSelection[0]));
            Spot spot = (Spot)e.CurrentSelection[0];
            Pin pin = new Pin()
            {
                Label = spot.Location.Address,
                Location = new Location(spot.Location.Latitude, spot.Location.Longitude)
            };
            _lblSelectedAddress.Text = pin.Label;
            _cvMap.Pins.Clear();
            _cvMap.Pins.Add(pin);
            MapSpan span = new MapSpan(pin.Location, 0.01, 0.01);
            _cvMap.MoveToRegion(span);
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