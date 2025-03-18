using System.Threading.Tasks;

namespace eatMeet.Utilities;

public static class UserInterface
{
    public static async Task DisplayPopUp_Regular(string title, string message, string button)
    {
        Page? page = Application.Current?.Windows[0]?.Page;
        if (page != null)
        {
            await page.DisplayAlert(title, message, button);
        }
    }

    public static async Task<bool> DisplayPopPup_Choice(string title, string message, string button_True, string button_False)
    {
        Page? page = Application.Current?.Windows[0]?.Page;
        if (page != null)
        {
            return await page.DisplayAlert(title, message, button_True, button_False);
        }

        return false;
    }
}
