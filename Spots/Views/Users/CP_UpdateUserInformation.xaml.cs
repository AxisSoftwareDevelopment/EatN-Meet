using System.Globalization;
using eatMeet.Models;
using eatMeet.ResourceManager;
using eatMeet.Database;
using eatMeet.Utilities;

namespace eatMeet;

public partial class CP_UpdateUserInformation : ContentPage
{
	private Client _user;
	private bool _userIsEmpty;
    private string _password, _email;
    private bool _birhtdateSelected, _profilePictureChanged = false;
    private ImageFile? _profilePictureFile = null;

    public CP_UpdateUserInformation(Client user, string email = "", string password = "")
	{
        _user = user;
        _userIsEmpty = !password.Equals("") && !email.Equals("") && !user.UserDataRetrieved;
        _password = password;
        _email = email;

        DisplayInfo displayInfo = DeviceDisplay.MainDisplayInfo;
        double profilePictureDimensions = displayInfo.Height * 0.065;

        InitializeComponent();

        _FrameProfilePicture.HeightRequest = profilePictureDimensions;
        _FrameProfilePicture.WidthRequest = profilePictureDimensions;

        if (_userIsEmpty)
            NavigationPage.SetHasBackButton(this, false);

        InitializeControllers();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_userIsEmpty)
            return true;
        else
            return base.OnBackButtonPressed();
    }

    private void SkipOnClick(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    private async void SaveOnCLickAsync(object sender, EventArgs e)
    {
        try
        {
            LockInputs();

            if (Application.Current == null)
            {
                return;
            }

            Client newData = new()
            {
                FirstName = ToTitleCase(_entryFirstName.Text.Trim()),
                LastName = ToTitleCase(_entryLastName.Text.Trim()),
                Description = _editorDescription.Text.Trim(),
                PhoneNumber = _entryPhoneNumber.Text,
                PhoneCountryCode = _entryPhoneCountryCode.Text,
                BirthDate = _dateBirthdate.Date
            };
            
            if (ValidateFields(newData))
            {
                HideErrorSection();

                string profilePictureAddress = "";
                if (_userIsEmpty)
                {
                    _user.Email = _email;
                }
                if(DataChanged(newData))
                {
                    _user.FirstName = newData.FirstName;
                    _user.LastName = newData.LastName;
                    if (_birhtdateSelected)
                        _user.BirthDate = newData.BirthDate;
                    _user.PhoneNumber = newData.PhoneNumber;
                    _user.PhoneCountryCode = newData.PhoneCountryCode;
                    _user.Description = newData.Description;
                    if (_profilePictureChanged && _profilePictureFile != null)
                    {
                        profilePictureAddress = await DatabaseManager.SaveFile($"Users/{_user.UserID}", "ProfilePicture", _profilePictureFile);
                        _user.ProfilePictureSource = ImageSource.FromStream(() => ImageManagement.ByteArrayToStream(_profilePictureFile.Bytes?? []));
                    }

                    if (await DatabaseManager.SaveUserDataAsync(_user, profilePictureAddress))
                    {
                        await UserInterface.DisplayPopUp_Regular("Success", "Your information has been updated. Way to go!", "OK");
                        // If the business was empty, it meas we came from the log in.
                        if (_userIsEmpty)
                        {
                            // We then have to log in and go to main page.
                            await DatabaseManager.LogInUserAsync(_email, _password, getUser: false);
                            Application.Current.Windows[0].Page = new FP_MainShell(_user);
                        }
                        else
                        {
                            // If the business was just updating information, then we just pop the page from navigation
                            SessionManager.CurrentSession?.UpdateUserData(_user);

                            await Navigation.PopAsync();
                        }
                    }
                }
                else
                {
                    // If the business was updating information, but didnt change any data, we do nothing
                    await UserInterface.DisplayPopUp_Regular("Alert", "No information was changed", "OK");
                    await Navigation.PopAsync();
                }
            }
        }
        catch (Exception ex)
        {
            await UserInterface.DisplayPopUp_Regular("Unhandled Error", ex.Message + "\n" + ex.StackTrace, "OK");
        }
        finally
        {
            UnlockInputs();
        }
    }

    public async void LoadImageOnClickAsync(object sender, EventArgs e)
    {
        ImageFile? image = await ImageManagement.PickImageFromInternalStorage();

        if (image != null) 
        {
            _profilePictureFile = image;
            _ProfileImage.Source = ImageSource.FromStream( () => ImageManagement.ByteArrayToStream(image.Bytes?? []) );
            _profilePictureChanged = true;
        }
        
    }

    #region Utilities
    private void InitializeControllers()
    {
        // Load _user data
        _entryFirstName.Text = _user.FirstName;
        _entryLastName.Text = _user.LastName;
        _entryPhoneNumber.Text = _user.PhoneNumber;
        _entryPhoneCountryCode.Text = _user.PhoneCountryCode;
        _editorDescription.Text = _user.Description;
        _ProfileImage.Source = _user.ProfilePictureSource;
        // Initialize BirthDate field
        if (_userIsEmpty)
        {
            _dateBirthdate.Format = "--/--/----";
            _dateBirthdate.MaximumDate = DateTime.Today;
            _dateBirthdate.DateSelected += (o, e) =>
            {
                _dateBirthdate.Format = "MM/dd/yyyy";
                _dateBirthdate.SetDynamicResource(DatePicker.TextColorProperty, "SecondaryAccent");
                _birhtdateSelected = true;
            };
        }
        else
        {
            _dateBirthdate.Date = _user.BirthDate.Date;
        }
    }

    private bool ValidateFields(Client newData)
    {
        bool thereAreEmptyFields = newData.FirstName.Length == 0 ||
                            newData.LastName.Length == 0 ||
                            (!_birhtdateSelected && _userIsEmpty);
        bool descriptionUnder150Chars = newData.Description.Length <= 150;
        bool validPhoneNumber = (newData.PhoneNumber.Length == 10 && newData.PhoneCountryCode.Length == 2) || (newData.PhoneNumber.Length == 0 && newData.PhoneCountryCode.Length == 0);
        bool birthdateIsValid = (DateTime.Today.Year - _dateBirthdate.Date.Year) > 12;

        if (thereAreEmptyFields && !birthdateIsValid && !descriptionUnder150Chars && !validPhoneNumber)
        {
            string errorMessageID = "txt_Error_UnkownError";

            #region Error message calculation
            if (thereAreEmptyFields)
            {
                errorMessageID = "txt_RegisterError_EmptyFields";
            }
            else if (!birthdateIsValid)
            {
                errorMessageID = "txt_UserInfoError_InvalidBirthdate";
            }
            else if (!descriptionUnder150Chars)
            {
                errorMessageID = "txt_UserInfoError_DescriptionTooLong";
            }
            else if (!validPhoneNumber)
            {
                errorMessageID = "txt_UserInfoError_InvalidPhoneNumber";
            }
            #endregion

            DisplayErrorSection(errorMessageID);

            return false;
        }

        return true;
    }
    private bool DataChanged(Client newData)
    {
        if(_profilePictureChanged)
            return true;
        if (_user.FirstName != newData.FirstName)
            return true;
        if (_user.LastName != newData.LastName)
            return true;
        if (_user.Description != newData.Description)
            return true;
        if (_user.PhoneNumber != newData.PhoneNumber)
            return true;
        if(_user.PhoneCountryCode != newData.PhoneCountryCode)
            return true;
        if (_birhtdateSelected)
            return true;

        return false;
    }

    private void DisplayErrorSection(string errorID)
    {
        _lblRegisterError.SetDynamicResource(Label.TextProperty, errorID);
        _lblRegisterError.IsVisible = true;
    }

    private void HideErrorSection()
    {
        _lblRegisterError.IsVisible = false;
    }

    private string ToTitleCase(string inputText)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(inputText.ToLower());
    }

    private void LockInputs()
    {
        _btnLoadImage.IsEnabled = false;
        _btnSave.IsEnabled = false;
        _editorDescription.IsEnabled = false;
        _entryFirstName.IsEnabled = false;
        _entryLastName.IsEnabled = false;
        _entryPhoneCountryCode.IsEnabled = false;
        _entryPhoneNumber.IsEnabled = false;
    }

    private void UnlockInputs()
    {
        _btnLoadImage.IsEnabled = true;
        _btnSave.IsEnabled = true;
        _editorDescription.IsEnabled = true;
        _entryFirstName.IsEnabled = true;
        _entryLastName.IsEnabled = true;
        _entryPhoneCountryCode.IsEnabled = true;
        _entryPhoneNumber.IsEnabled = true;
    }
    #endregion
}