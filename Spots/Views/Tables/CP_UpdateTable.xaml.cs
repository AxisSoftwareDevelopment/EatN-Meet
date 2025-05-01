using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Globalization;

using eatMeet.Models;
using eatMeet.ResourceManager;
using eatMeet.Database;
using eatMeet.Utilities;

namespace eatMeet;

public partial class CP_UpdateTable : ContentPage
{
	private Table _Table;
    private bool _inputsAreLocked;
    private bool _profilePictureChanged = false;
    private bool _locationChanged = false;
    private ImageFile? _profilePictureFile = null;
    public CP_UpdateTable(Table? table = null)
	{
        _inputsAreLocked = false;
        _Table = table ?? new() { TableMembers = [SessionManager.CurrentSession?.Client?.UserID] };

        DisplayInfo displayInfo = DeviceDisplay.MainDisplayInfo;
        double tablePictureDimensions = displayInfo.Height * 0.065;

        InitializeComponent();

        if (LocationManager.CurrentLocation != null)
        {
            _cvMiniMap.Pins.Clear();
            _cvMiniMap.MoveToRegion(new MapSpan(LocationManager.CurrentLocation, 0.01, 0.01));
            _cvMiniMap.Pins.Add(new Pin() { Label = "", Location = LocationManager.CurrentLocation });
        }

        _FrameTablePicture.HeightRequest = tablePictureDimensions;
        _FrameTablePicture.WidthRequest = tablePictureDimensions;
        _cvMiniMap.HeightRequest = tablePictureDimensions * 0.75;
        _cvMiniMap.MapClicked += _cvMiniMap_MapClicked;
    }

    private void _cvMiniMap_MapClicked(object? sender, MapClickedEventArgs e)
    {
        if (!_inputsAreLocked)
        {
            Navigation.PushAsync(new CP_MapLocationSelector(() => _cvMiniMap.VisibleRegion, _entryAddress.Text ?? ""));
        }
    }

    private async void SaveOnCLickAsync(object sender, EventArgs e)
    {
        LockInputs();
        if (Application.Current == null)
        {
            return;
        }

        Table newData = new Table()
        {
            TableName = ToTitleCase(_entryTableName.Text.Trim()),
            Location = new FirebaseLocation(_entryAddress.Text.Trim(), 0, 0),
            Description = _editorDescription.Text.Trim(),
        };

        if (ValidateFields(newData))
        {
            HideErrorSection();

            if (DataChanged(newData))
            {
                //string profilePictureAddress = "";
                _Table.TableName = newData.TableName;
                _Table.Description = newData.Description;
                Location locationSelected = _cvMiniMap.Pins[0].Location;
                _Table.Location = new FirebaseLocation(newData.Location.Address, locationSelected.Latitude, locationSelected.Longitude);

                // If profile picture was changed we pass the picture file (even if its null, to remove the picture reference) otherwise we pass null.
                if (_Table.TableMembers.Count > 0
                    && await DatabaseManager.SaveTableDataAsync(_Table, _profilePictureChanged ? _profilePictureFile : null))
                {
                    await UserInterface.DisplayPopUp_Regular("Success", "The information has been updated. Way to go!", "OK");
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
            _TableImage.Source = ImageSource.FromStream(() => ImageManagement.ByteArrayToStream(image.Bytes ?? []));
            _profilePictureChanged = true;
        }

    }

    #region Utilities
    private void LockInputs()
    {
        _btnLoadImage.IsEnabled = false;
        _btnSave.IsEnabled = false;
        _editorDescription.IsEnabled = false;
        _entryAddress.IsEnabled = false;
        _entryTableName.IsEnabled = false;
        _inputsAreLocked = true;
    }

    private void UnlockInputs()
    {
        _btnLoadImage.IsEnabled = true;
        _btnSave.IsEnabled = true;
        _editorDescription.IsEnabled = true;
        _entryAddress.IsEnabled = true;
        _entryTableName.IsEnabled = true;
        _inputsAreLocked = false;
    }

    private static string ToTitleCase(string inputText)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(inputText.ToLower());
    }

    private bool ValidateFields(Table table)
    {
        bool thereAreEmptyFields = table.TableName.Length == 0 ||
                            table.Location.Address.Length == 0;
        bool validLocationSelected = _cvMiniMap.Pins.Count == 1;
        bool descriptionUnder150Chars = table.Description.Length <= 150;

        if (thereAreEmptyFields || !descriptionUnder150Chars || !validLocationSelected)
        {
            string errorMessageID = "txt_Error_UnkownError";

            #region Error message calculation
            if (!validLocationSelected)
            {
                errorMessageID = "txt_TableError_NoValidLocationSelected";
            }
            else if (thereAreEmptyFields)
            {
                errorMessageID = "txt_TableError_EmptyFields";
            }
            else if (!descriptionUnder150Chars)
            {
                errorMessageID = "txt_TableInfoError_DescriptionTooLong";
            }
            #endregion

            DisplayErrorSection(errorMessageID);

            return false;
        }

        return true;
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

    private bool DataChanged(Table table)
    {
        if (_profilePictureChanged)
            return true;
        if (_locationChanged)
            return true;
        if (_Table.TableName != table.TableName)
            return true;
        if (_Table.Description != table.Description)
            return true;
        if (_Table.Location.Address != table.Location.Address)
            return true;

        return false;
    }
    #endregion
}