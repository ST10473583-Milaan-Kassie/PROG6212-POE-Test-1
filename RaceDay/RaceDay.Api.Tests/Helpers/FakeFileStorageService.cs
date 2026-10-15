using Microsoft.AspNetCore.Http;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.Helpers;

//stands in for blob storage so the tests dont need azurite running
//keeps a list of whats been uploaded and deleted so tests can check the controller called it properly
public class FakeFileStorageService : IFileStorageService
{
    public List<string> UploadedUrls { get; } = new();

    public List<string> DeletedUrls { get; } = new();

    public Task<string> UploadAsync(IFormFile file, string folder)
    {
        //same shape of url the real one gives back, .test is a made up domain that can never be real
        var url = $"https://storage.test/raceday-images/{folder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        UploadedUrls.Add(url);
        return Task.FromResult(url);
    }

    public Task DeleteAsync(string fileUrl)
    {
        DeletedUrls.Add(fileUrl);
        return Task.CompletedTask;
    }
}