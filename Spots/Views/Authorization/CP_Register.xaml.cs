using System.Text.RegularExpressions;

using eatMeet.Database;
using eatMeet.Utilities;

namespace eatMeet;

public partial class CP_Register : ContentPage
{
	public CP_Register()
	{
        InitializeComponent();
    }

    public async void BtnRegisterOnClick(Object sender, EventArgs e)
    {
        LockInputs();

        string email = _entryEmail.Text is null ? "" : _entryEmail.Text;
        string confirmEmail = _entryConfirmEmail is null ? "" : _entryConfirmEmail.Text;
        string password = _entryPassword.Text is null ? "" : _entryPassword.Text;
        string confirmPassword = _entryConfirmPassword.Text is null ? "" : _entryConfirmPassword.Text;

        bool thereAreEmptyFields = (email.Length == 0 ||
                            confirmEmail.Length == 0 ||
                            confirmPassword.Length == 0);
        bool emailIsValid = ValidateEmail(email);
        bool emailsMatch = email.Equals(confirmEmail);
        bool passwrodIsValid = password.Length > 7;
        bool passwordsMatch = password.Equals(confirmPassword);

        if (!thereAreEmptyFields && emailIsValid && emailsMatch && passwrodIsValid && passwordsMatch)
        {
            HideErrorSection();

            try
            {
                if (await DatabaseManager.CreateUserAsync(email, password))
                {
                    await UserInterface.DisplayPopUp_Regular("Success", "User created successfully.", "OK");
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    DisplayErrorSection("txt_RegisterError_RegisterError");
                }
            }
            catch (Exception ex)
            {
                await UserInterface.DisplayPopUp_Regular("Error", ex.Message.ToString(), "OK");
            }
        }
        else
        {
            string errorMessageID = "Error on input fields";

            #region Error message calculation
            if (thereAreEmptyFields)
            {
                errorMessageID = "txt_RegisterError_EmptyFields";
            }
            else if (!emailIsValid)
            {
                errorMessageID = "txt_RegisterError_InvalidEmail";
            }
            else if (!emailsMatch)
            {
                errorMessageID = "txt_RegisterError_EmailsDontMatch";
            }
            else if (!passwrodIsValid)
            {
                errorMessageID = "txt_RegisterError_InvalidPasswordLength";
            }
            else if (!passwordsMatch)
            {
                errorMessageID = "txt_RegisterError_PasswordsDontMatch";
            }
            #endregion

            DisplayErrorSection(errorMessageID);
        }

        UnlockInputs();
    }

    #region Utilites
    private bool ValidateEmail(string email)
    {
        Match eMailIsValid = Regex.Match(email, @"^([a-zA-Z0-9_\.-]+)@([\da-z\.-]+)\.([a-z\.]{2,5})$");

        return eMailIsValid.Success;
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

    private void LockInputs()
    {
        _btnRegister.IsEnabled = false;
        _entryEmail.IsEnabled = false;
        _entryConfirmEmail.IsEnabled = false;
        _entryPassword.IsEnabled = false;
        _entryConfirmPassword.IsEnabled = false;
        _loadingOverlay.Show();
    }

    private void UnlockInputs()
    {
        _loadingOverlay.Hide();
        _btnRegister.IsEnabled = true;
        _entryEmail.IsEnabled = true;
        _entryConfirmEmail.IsEnabled = true;
        _entryPassword.IsEnabled = true;
        _entryConfirmPassword.IsEnabled = true;
    }
    #endregion
}