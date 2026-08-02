using eatMeet.Models;
using eatMeet.Utilities;
using eatMeet.Database;
using eatMeet.GooglePlacesService;

namespace eatMeet;

public partial class CP_SearchPage : ContentPage
{
    private enum ESearchFocus
    {
        CLIENT,
        SPOT
    };
    private ESearchFocus CurrentFilterApplyed = ESearchFocus.CLIENT;
	private readonly FeedContext<object> SearchResultsListContext = new();
    private readonly DebouncedAction<string> DebouncedSearch;
    private bool _LoadingResults = false;
    private bool _IsLoadingMoreResults = false;
    private bool _HasMoreResults = true;
    private string _CurrentSearchText = string.Empty;
    private Client? _LastClientFetched = null;
    private bool _CanShowMoreResults = false;

    public bool CanShowMoreResults
    {
        get => _CanShowMoreResults;
        set
        {
            _CanShowMoreResults = value;
            OnPropertyChanged(nameof(CanShowMoreResults));
        }
    }

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
    public CP_SearchPage()
	{
		InitializeComponent();
		
        _actLoadingIndicator.BindingContext = this;
        _btnShowMore.BindingContext = this;
        _colSearchBarCollectionView.BindingContext = SearchResultsListContext;
        _colSearchBarCollectionView.ItemsSource = SearchResultsListContext.ItemSource;
        _colSearchBarCollectionView.SelectionChanged += _colSearchBarCollectionView_SelectionChanged;

        DebouncedSearch = new (RefreshSearchResults);
        _entrySearchTerms.TextChanged += async (sender, e) =>
        {
            if(!LoadingResults)
            {
                LoadingResults = true;
            }
            await DebouncedSearch.Run(e.NewTextValue);
        };

        _rbtnClientFilter.CheckedChanged += _rbtnClientFilter_CheckedChanged;
        _rbtnSpotFilet.CheckedChanged += _rbtnSpotFilet_CheckedChanged;
	}

    private async void _rbtnClientFilter_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            if (!LoadingResults)
            {
                LoadingResults = true;
            }
            CurrentFilterApplyed = ESearchFocus.CLIENT;
            await DebouncedSearch.Run(_entrySearchTerms.Text);
        }
    }

    private async void _rbtnSpotFilet_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if(e.Value)
        {
            if (!LoadingResults)
            {
                LoadingResults = true;
            }
            CurrentFilterApplyed = ESearchFocus.SPOT;
            await DebouncedSearch.Run(_entrySearchTerms.Text);
        }
    }


    private async void _colSearchBarCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if(e.CurrentSelection.Count > 0)
        {
            if (CurrentFilterApplyed == ESearchFocus.CLIENT)
            {
                await ((Client)e.CurrentSelection[0]).OpenClientView(Navigation);
            }
            else
            {
                await Navigation.PushAsync(new CP_SpotView((Spot)e.CurrentSelection[0]));
            }
            _colSearchBarCollectionView.SelectedItem = null;
        }
    }

    private async void OnShowMoreClicked(object? sender, EventArgs e)
    {
        await LoadMoreResults();
    }

    private async Task LoadMoreResults()
    {
        if (_IsLoadingMoreResults || !_HasMoreResults || string.IsNullOrWhiteSpace(_CurrentSearchText))
        {
            return;
        }

        _IsLoadingMoreResults = true;
        CanShowMoreResults = false;

        try
        {
            List<object> nextItems = [];

            if (CurrentFilterApplyed == ESearchFocus.CLIENT)
            {
                List<Client> clients = await DatabaseManager.FetchClients_Filtered(
                    nameSearchTerms: [_CurrentSearchText],
                    currentUsrID_ToAvoid: SessionManager.CurrentSession?.Client?.UserID,
                    lastClient: _LastClientFetched);

                clients = clients.GroupBy(c => c.UserID).Select(g => g.First()).ToList();
                if (clients.Count == 0)
                {
                    _HasMoreResults = false;
                }
                else
                {
                    _LastClientFetched = clients.Last();
                    clients.ForEach(nextItems.Add);
                }
            }
            else
            {
                List<Spot> spotList = await GooglePlaces.GetAllRestaurants_NextPage();
                if (spotList.Count == 0)
                {
                    _HasMoreResults = false;
                }
                else
                {
                    spotList.ForEach(nextItems.Add);
                }
            }

            if (nextItems.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(() => SearchResultsListContext.AddElements(nextItems));
            }
        }
        finally
        {
            _IsLoadingMoreResults = false;
            CanShowMoreResults = _HasMoreResults && SearchResultsListContext.ItemSource.Count > 0;
        }
    }

    private async Task RefreshSearchResults(string searchInput)
    {
        string trimmedSearch = searchInput?.Trim() ?? string.Empty;
        _CurrentSearchText = trimmedSearch.ToUpperInvariant();
        _HasMoreResults = true;
        _LastClientFetched = null;
        _IsLoadingMoreResults = false;

        string[] inputs = !string.IsNullOrWhiteSpace(trimmedSearch) ? [_CurrentSearchText] : [];
        if (inputs.Length > 0)
        {
            List<object> list = [];
            if(CurrentFilterApplyed == ESearchFocus.CLIENT)
            {
                List<Client> clients = await DatabaseManager.FetchClients_Filtered(nameSearchTerms: inputs, currentUsrID_ToAvoid: SessionManager.CurrentSession?.Client?.UserID);
                // Remove duplicates by UserID
                clients = clients.GroupBy(c => c.UserID).Select(g => g.First()).ToList();
                if (clients.Count > 0)
                {
                    _LastClientFetched = clients.Last();
                }
                clients.ForEach(list.Add);
            }
            else
            {
                Location? location = LocationManager.CurrentLocation ?? await LocationManager.GetUpdatedLocaionAsync();
                if (location != null)
                {
                    List<Spot> spotList = await GooglePlaces.GetAllRestaurants(trimmedSearch.ToUpperInvariant(), location, 1000, 5);
                    spotList.ForEach(list.Add);
                    _HasMoreResults = spotList.Count > 0;
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                SearchResultsListContext.RefreshFeed(list);
                CanShowMoreResults = _HasMoreResults && SearchResultsListContext.ItemSource.Count > 0;
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SearchResultsListContext.RefreshFeed([]);
                CanShowMoreResults = false;
            });
        }

        LoadingResults = false;
    }
}