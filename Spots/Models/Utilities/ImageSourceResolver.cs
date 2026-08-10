using eatMeet.Database;
using Microsoft.Maui.Controls;

namespace eatMeet.Utilities;

public static class ImageSourceResolver
{
    public static ImageSource Resolve(string? address, string fallbackFile)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return ImageSource.FromFile(fallbackFile);
        }

        if (Uri.TryCreate(address, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFile))
        {
            return ImageSource.FromUri(uri);
        }

        return ImageSource.FromFile(fallbackFile);
    }

    /// <summary>
    /// Resolves an image address (either a Firebase Storage path or an already-resolved
    /// Google Places API photo URL) into an <see cref="ImageSource"/>, downloading the link
    /// via <see cref="DatabaseManager.GetImageDownloadLink"/> when needed. Falls back to the
    /// provided local file when the address is empty.
    /// </summary>
    public static async Task<ImageSource> ResolveAsync(string? address, string fallbackFile)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return ImageSource.FromFile(fallbackFile);
        }

        string downloadAddress = await DatabaseManager.GetImageDownloadLink(address);
        return ImageSource.FromUri(new Uri(downloadAddress));
    }
}
