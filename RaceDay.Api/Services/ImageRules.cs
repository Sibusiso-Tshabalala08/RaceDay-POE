namespace RaceDay.Api.Services;

/// <summary>
/// Validates an uploaded image BEFORE it is sent to Azure Blob Storage:
/// size limit, allowed content type, and the file's real signature ("magic
/// bytes"), so a renamed .exe or text file cannot be stored as an image.
/// </summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> ExtensionByContentType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif"
        };

    public static async Task<(bool Ok, string? Error, string Extension)> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return (false, "Please choose an image file to upload.", string.Empty);

        if (file.Length > MaxBytes)
            return (false, "The image must be 5 MB or smaller.", string.Empty);

        if (!ExtensionByContentType.TryGetValue(file.ContentType ?? string.Empty, out var extension))
            return (false, "Only JPG, PNG, WEBP or GIF images are allowed.", string.Empty);

        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            await stream.ReadAtLeastAsync(header.AsMemory(), header.Length, throwOnEndOfStream: false);
        }

        if (!MatchesSignature(header, extension))
            return (false, "The file content is not a valid image of the type it claims to be.", string.Empty);

        return (true, null, extension);
    }

    private static bool MatchesSignature(byte[] h, string extension) => extension switch
    {
        ".jpg" => h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF,
        ".png" => h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47
                  && h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A,
        ".gif" => h[0] == (byte)'G' && h[1] == (byte)'I' && h[2] == (byte)'F' && h[3] == (byte)'8',
        ".webp" => h[0] == (byte)'R' && h[1] == (byte)'I' && h[2] == (byte)'F' && h[3] == (byte)'F'
                   && h[8] == (byte)'W' && h[9] == (byte)'E' && h[10] == (byte)'B' && h[11] == (byte)'P',
        _ => false
    };
}
