using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Globalization;

using eatMeet.Models;
using eatMeet.ResourceManager;
using eatMeet.Database;
using eatMeet.Utilities;

namespace eatMeet;

public partial class CP_UpdateSpot : ContentPage
{
    private Spot _spot;
    private bool _inputsAreLocked;
    private bool _profilePictureChanged = false;
    private bool _locationChanged = false;
    private ImageFile? _profilePictureFile = null;
    private readonly FeedContext<ListItemAddress> SearchBoxContext = new();
    private readonly DebouncedAction<string> DebouncedSearch;

    public CP_UpdateSpot(Spot? spot = null)
    {
        _inputsAreLocked = false;
        _spot = spot ?? new();

        DisplayInfo displayInfo = DeviceDisplay.MainDisplayInfo;
        double profilePictureDimensions = displayInfo.Height * 0.065;

        InitializeComponent();

        if (LocationManager.CurrentLocation != null)
        {
            _cvMiniMap.Pins.Clear();
            _cvMiniMap.MoveToRegion(new MapSpan(LocationManager.CurrentLocation, 0.01, 0.01));
            _cvMiniMap.Pins.Add(new Pin() { Label = "", Location = LocationManager.CurrentLocation });
        }

        _colSearchBarResults.BindingContext = SearchBoxContext;
        _colSearchBarResults.MaximumHeightRequest = profilePictureDimensions * 1;
        _colSearchBarResults.SelectionChanged += _colSearchBarResults_SelectionChanged;

        DebouncedSearch = new ( RefreshSearchResults );
        _entryAddress.TextChanged += async (sender, e) =>
        {
            await DebouncedSearch.Run(e.NewTextValue);
        };

        _FrameProfilePicture.HeightRequest = profilePictureDimensions;
        _FrameProfilePicture.WidthRequest = profilePictureDimensions;
        _cvMiniMap.HeightRequest = profilePictureDimensions * 0.75;
        _cvMiniMap.MapClicked += _cvMiniMap_MapClicked;

        InitializeControllers();
    }

    private void _colSearchBarResults_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        LoadSelectedAddress((ListItemAddress)e.CurrentSelection[0]);
    }

    private void LoadSelectedAddress(ListItemAddress itemAddress)
    {
        _lblSelectedAdress.Text = itemAddress.Address;
        _cvMiniMap.Pins.Clear();
        _cvMiniMap.MoveToRegion(new MapSpan(itemAddress.Location, 0.01, 0.01));
        _cvMiniMap.Pins.Add(new Pin() { Label = "", Location = itemAddress.Location });
    }

    private async Task RefreshSearchResults(string? searchInput)
    {
        try
        {
            if ((searchInput?.Length ?? 0) > 0)
            {
                List<ListItemAddress> adreesses = await LocationManager.GetAddressesFromAddress(searchInput ?? "");
                SearchBoxContext.RefreshFeed(adreesses);
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
    }

    private void _cvMiniMap_MapClicked(object? sender, MapClickedEventArgs e)
    {
        if (!_inputsAreLocked)
        {
            Navigation.PushAsync(new CP_MapLocationSelector(() => _cvMiniMap.VisibleRegion, _lblSelectedAdress.Text ?? ""));
        }
    }

    private void SkipOnClick(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    private async void SaveOnCLickAsync(object sender, EventArgs e)
    {
        LockInputs();
        if (Application.Current == null)
        {
            return;
        }

        Spot newData = new Spot()
        {
            Name = ToTitleCase(_entryName.Text.Trim()),
            Location = new FirebaseLocation(_lblSelectedAdress.Text.Trim(), 0, 0),
            Description = _editorDescription.Text.Trim(),
            PhoneNumber = _entryPhoneNumber.Text,
            PhoneCountryCode = _entryPhoneCountryCode.Text
        };

        if (ValidateFields(newData))
        {
            HideErrorSection();

            if (DataChanged(newData))
            {
                //string profilePictureAddress = "";
                _spot.PhoneNumber = newData.PhoneNumber;
                _spot.PhoneCountryCode = newData.PhoneCountryCode;
                _spot.Name = newData.Name;
                _spot.Description = newData.Description;
                Location locationSelected = _cvMiniMap.Pins[0].Location;
                _spot.Location = new FirebaseLocation(newData.Location.Address, locationSelected.Latitude, locationSelected.Longitude);

                // If profile picture was changed we pass the picture file (even if its null, to remove the picture reference) otherwise we pass null.
                if (await DatabaseManager.SaveSpotDataAsync(_spot, _profilePictureChanged ? _profilePictureFile : null))
                {
                    await UserInterface.DisplayPopUp_Regular("Success", "Your information has been updated. Way to go!", "OK");
                    await Navigation.PopAsync();
                }
            }
            else
            {
                // If the business was updating information, but didnt change any data, we do nothing
                await UserInterface.DisplayPopUp_Regular("Alert", "No information was changed", "OK");
                await Navigation.PopAsync();
            }

        }
        UnlockInputs();
    }

    public async void LoadImageOnClickAsync(object sender, EventArgs e)
    {
        ImageFile? image = await ImageManagement.PickImageFromInternalStorage();

        if (image != null)
        {
            _profilePictureFile = image;
            _ProfileImage.Source = ImageSource.FromStream(() => ImageManagement.ByteArrayToStream(image.Bytes ?? []));
            _profilePictureChanged = true;
        }

    }

    #region Utilities
    private void InitializeControllers()
    {
        // Load _spot data
        _entryName.Text = _spot.Name;
        _editorDescription.Text = _spot.Description;
        _ProfileImage.Source = _spot.ProfilePictureSource;
        _entryAddress.Text = _spot.Location.Address;
        _cvMiniMap.MoveToRegion(new MapSpan(_spot.Geolocation, 0.01, 0.01));
        _cvMiniMap.Pins.Clear();
        _cvMiniMap.Pins.Add(new Pin()
        {
            Label = _spot.Location.Address,
            Location = _spot.Geolocation
        });
        _entryPhoneNumber.Text = _spot.PhoneNumber;
        _entryPhoneCountryCode.Text = _spot.PhoneCountryCode;
    }

    private bool ValidateFields(Spot business)
    {
        bool thereAreEmptyFields = business.Name.Length == 0 ||
                            business.Location.Address.Length == 0;
        bool validLocationSelected = _cvMiniMap.Pins.Count == 1;
        bool descriptionUnder150Chars = business.Description.Length <= 150;
        bool validPhoneNumber = (business.PhoneNumber.Length == 10 && business.PhoneCountryCode.Length == 2)
                || (business.PhoneNumber.Length == 0 && business.PhoneCountryCode.Length == 0);

        if (thereAreEmptyFields || !descriptionUnder150Chars || !validPhoneNumber || !validLocationSelected)
        {
            string errorMessageID = "txt_Error_UnkownError";

            #region Error message calculation
            if (!validLocationSelected)
            {
                errorMessageID = "txt_BussinessError_NoValidLocationSelected";
            }
            else if (thereAreEmptyFields)
            {
                errorMessageID = "txt_RegisterError_EmptyFields";
            }
            else if (!descriptionUnder150Chars)
            {
                errorMessageID = "txt_spotInfoError_DescriptionTooLong";
            }
            else if (!validPhoneNumber)
            {
                errorMessageID = "txt_spotInfoError_InvalidPhoneNumber";
            }
            #endregion

            DisplayErrorSection(errorMessageID);

            return false;
        }

        return true;
    }

    private bool DataChanged(Spot business)
    {
        if (_profilePictureChanged)
            return true;
        if (_locationChanged)
            return true;
        if (_spot.Name != business.Name)
            return true;
        if (_spot.Description != business.Description)
            return true;
        if (_spot.PhoneNumber != business.PhoneNumber)
            return true;
        if (_spot.PhoneCountryCode != business.PhoneCountryCode)
            return true;
        if (_spot.Location.Address != business.Location.Address)
            return true;

        return false;
    }

    private void DisplayErrorSection(string errorID)
    {
        _lblError.SetDynamicResource(Label.TextProperty, errorID);
        _lblError.IsVisible = true;
    }

    private void HideErrorSection()
    {
        _lblError.IsVisible = false;
    }

    private static string ToTitleCase(string inputText)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(inputText.ToLower());
    }

    private void LockInputs()
    {
        _btnLoadImage.IsEnabled = false;
        _btnSave.IsEnabled = false;
        _editorDescription.IsEnabled = false;
        _entryAddress.IsEnabled = false;
        _entryName.IsEnabled = false;
        _entryPhoneCountryCode.IsEnabled = false;
        _entryPhoneNumber.IsEnabled = false;
        _inputsAreLocked = true;
    }

    private void UnlockInputs()
    {
        _btnLoadImage.IsEnabled = true;
        _btnSave.IsEnabled = true;
        _editorDescription.IsEnabled = true;
        _entryAddress.IsEnabled = true;
        _entryName.IsEnabled = true;
        _entryPhoneCountryCode.IsEnabled = true;
        _entryPhoneNumber.IsEnabled = true;
        _inputsAreLocked = false;
    }
    #endregion
}