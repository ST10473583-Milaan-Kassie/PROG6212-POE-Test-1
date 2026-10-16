using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Tests.Helpers;

//makes pretend uploaded files for the upload tests, the same thing the browser would send as multipart/form-data
public static class TestFiles
{
    //the first 8 bytes every real png starts with, the rest of the file can be anything for these tests
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    //a png that passes every image check, size is the total number of bytes
    public static IFormFile Png(string fileName = "photo.png", long size = 64)
    {
        var bytes = new byte[size];
        PngSignature.CopyTo(bytes, 0);
        return Make(bytes, fileName);
    }

    //a plain text file, the wrong type altogether
    public static IFormFile Text(string fileName = "notes.txt")
    {
        return Make("just some text"u8.ToArray(), fileName);
    }

    //named like a png but the bytes inside arent a png, like a renamed program
    public static IFormFile FakePng(string fileName = "photo.png")
    {
        return Make("MZ this is not really an image"u8.ToArray(), fileName);
    }

    private static IFormFile Make(byte[] bytes, string fileName)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", fileName);
    }
}