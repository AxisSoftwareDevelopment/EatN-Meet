using Plugin.Firebase.Core.Exceptions;

using eatMeet.Models;
using eatMeet.Database;
using eatMeet.Utilities;
using eatMeet.ResourceManager;

namespace eatMeet;

public partial class CP_Login : ContentPage
{
	public CP_Login()
	{
        InitializeComponent();
        Resources = Application.Current?.Resources;
    }

	private async void BtnLogInOnClickAsync(object sender, EventArgs e)
	{
        SwitchLockViewState();

        string email = _entryEmail.Text;
        string password = _entryPassword.Text;

        if (CredentialsAreValid(email, password, out string errorMsg))
        {
            HideErrorSection();

            await LookForUserDataInDBAsync(email, password);
        }
        else
        {
            DisplayErrorSection(errorMsg);
        }

        SwitchLockViewState();
    }

    private async Task LookForUserDataInDBAsync(string email, string password)
    {
        try
        {
            if (Application.Current != null)
            {
                FirebaseLocation? location = LocationManager.CurrentLocation != null ?
                    new(LocationManager.CurrentLocation) : null;
                Client user = await DatabaseManager.LogInUserAsync(email, password, lastLocation: location);

                if (user.UserDataRetrieved)
                {
                    // Check if the _user has updated its basic information yet
                    await UserInterface.DisplayPopUp_Regular("Welcome!", "Welcome to spots!", "OK");

                    if (Application.Current != null)
                    {
                        Application.Current.Windows[0].Page = new FP_MainShell(user);
                    }
                }
                else
                {
                    // Setup User Data
                    string[] strings = ResourceManagement.GetStringResources(Resources, ["lbl_FirstTime", "txt_FirstTime", "lbl_Ok"]);
                    await UserInterface.DisplayPopUp_Regular(strings[0], strings[1], strings[2]);
                    await Navigation.PushAsync(new CP_UpdateUserInformation(user, email, password));
                }
            }
        }
        catch (FirebaseAuthException ex)
        {
            #region Error Message Calculation
            string errorID = ex.Reason switch
            {
                FIRAuthError.InvalidEmail => "txt_LogInError_WrongCredentials_User",
                FIRAuthError.WrongPassword => "txt_LogInError_WrongCredentials_User",
                FIRAuthError.InvalidCredential => "txt_LogInError_InvalidCredential",
                FIRAuthError.UserNotFound => "txt_LogInError_WrongCredentials_User",
                FIRAuthError.UserDisabled => "txt_LogInError_UserDisabled",
                FIRAuthError.EmailAlreadyInUse => ex.Message.Split("->")[0].Trim(),
                _ => "txt_LogInError_Undefined",
            };
            #endregion

            if(ex.Reason == FIRAuthError.UserDisabled)
            {
                string[] strings = ["lbl_EmailNotVeirified", "txt_LogInError_UserDisabled", "lbl_Resend", "lbl_Cancel"];
                strings = ResourceManagement.GetStringResources(Resources, strings);
                if(await UserInterface.DisplayPopPup_Choice(strings[0], strings[1], strings[2], strings[3]))
                {
                    await DatabaseManager.ResendEmailVerificationAsync();
                }
            }
            else
            {
                DisplayErrorSection(errorID);
            }

            await DatabaseManager.LogOutAsync(skipLocationUpdate: true);
        }
        catch (Exception ex)
        {
            await UserInterface.DisplayPopUp_Regular("Unhandled Error", ex.Message, "OK");
        }
    }

    public void BtnRegisterOnClick(object sender, EventArgs e)
	{
        Navigation.PushAsync(new CP_Register());
    }

    private void SwitchLockViewState()
    {
        _btnLogIn.IsEnabled = !_btnLogIn.IsEnabled;
        _btnRegister.IsEnabled = !_btnRegister.IsEnabled;
    }

    private void DisplayErrorSection(string errorID)
    {
        if(Resources.TryGetValue(errorID, out object error))
        {
            _lblSignInError.Text = error.ToString();
            _lblSignInError.IsVisible = true;
        }
        else
        {
            _lblSignInError.IsVisible = false;
        }
    }

    private void HideErrorSection()
    {
        _lblSignInError.IsVisible = false;
    }

    private static bool CredentialsAreValid(string email, string password, out string errorMessage)
    {
        bool credentialsAreValid = true;
        errorMessage = "";
        if (email.Length == 0 || password.Length == 0)
        {
            credentialsAreValid = false;
            errorMessage = "txt_LogInError_EmptyEntry";
        }
        return credentialsAreValid;
    }
}