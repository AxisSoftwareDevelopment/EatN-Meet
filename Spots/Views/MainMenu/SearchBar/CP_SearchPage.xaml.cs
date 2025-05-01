using eatMeet.Models;
using eatMeet.Utilities;
using eatMeet.Database;

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
    private readonly Action<string?> DebouncedSearch;

    public string SearchTextInput { get; set; } = "";
	public CP_SearchPage()
	{
		InitializeComponent();
		
		_colSearchBarCollectionView.BindingContext = SearchResultsListContext;
        _colSearchBarCollectionView.SelectionChanged += _colSearchBarCollectionView_SelectionChanged;

        DebouncedSearch = DebounceHelper.Debounce<string?>(async (searchText) =>
        {
            await RefreshSearchResults(searchText);

            // Show or hide the search results frame
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _frameSearchResults.IsVisible = !string.IsNullOrEmpty(searchText) && SearchResultsListContext.ItemSource.Count > 0;
            });
        });
        _entrySearchTerms.TextChanged += (sender, e) =>
        {
            DebouncedSearch(e.NewTextValue);
        };

        _rbtnClientFilter.CheckedChanged += _rbtnClientFilter_CheckedChanged;
        _rbtnSpotFilet.CheckedChanged += _rbtnSpotFilet_CheckedChanged;
	}

    private void _rbtnClientFilter_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            CurrentFilterApplyed = ESearchFocus.CLIENT;
            DebouncedSearch(_entrySearchTerms.Text);
        }
    }

    private void _rbtnSpotFilet_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if(e.Value)
        {
            CurrentFilterApplyed = ESearchFocus.SPOT;
            DebouncedSearch(_entrySearchTerms.Text);
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

    private async Task RefreshSearchResults(string? searchInput)
    {
        string[] inputs = searchInput != null ? [searchInput.ToUpper().Trim()] : [];
        if (inputs.Length > 0)
        {
            List<object> list = [];
            if(CurrentFilterApplyed == ESearchFocus.CLIENT)
            {
                List<Client> spots = await DatabaseManager.FetchClients_Filtered(nameSearchTerms: inputs, currentUsrID_ToAvoid: SessionManager.CurrentSession?.Client?.UserID);
                spots.ForEach(list.Add);
            }
            else
            {
                List<Spot> spots = await DatabaseManager.FetchSpots_Filtered(filterParams: inputs);
                spots.ForEach(list.Add);
            }

            SearchResultsListContext.RefreshFeed(list);
        }
        else
        {
            SearchResultsListContext.RefreshFeed([]);
        }
    }
}