using System.Text;

namespace RaceDay.Api.Helpers;

//checks uploaded images before theyre stored, jpg png or webp up to 5 mb like the plan says
public static class ImageValidation
{
    //5 mb in bytes
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    //each allowed extension and its content type, the content type gets saved with the file so browsers show it properly
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".webp", "image/webp" }
    };

    //returns an error message if somethings wrong, or null if the file is fine
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "Please upload an image file.";
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return "Images can be no larger than 5 MB.";
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedTypes.ContainsKey(extension))
        {
            return "Only JPG, PNG, or WEBP images are allowed.";
        }

        //a renamed file (e.g. a program saved as photo.png) gets caught here
        if (!HasImageSignature(file, extension))
        {
            return "The file is not a valid JPG, PNG or WEBP image.";
        }

        return null;
    }

    //the content type to save with the file, only call this after validate passed
    public static string GetContentType(string fileName)
    {
        return AllowedTypes[Path.GetExtension(fileName)];
    }

    //every real jpg, png and webp starts with a known set of bytes, this checks they match the extension
    private static bool HasImageSignature(IFormFile file, string extension)
    {
        var header = new byte[12];

        using var stream = file.OpenReadStream();
        var bytesRead = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

        return extension.ToLowerInvariant() switch
        {
            //jpg starts with FF D8 FF
            ".jpg" or ".jpeg" => bytesRead >= 3
                && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,

            //png starts with 89 then the letters PNG and 4 more fixed bytes
            ".png" => bytesRead >= 8
                && header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),

            //webp starts with RIFF, then 4 bytes for the size, then WEBP
            ".webp" => bytesRead >= 12
                && Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                && Encoding.ASCII.GetString(header, 8, 4) == "WEBP",

            _ => false
        };
    }
}