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
}
