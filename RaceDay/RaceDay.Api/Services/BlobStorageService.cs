using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Services;

//saves uploaded images to azure blob storage, azurite stands in for it on this pc
public class BlobStorageService : IFileStorageService
{
    //all raceday images go in one container, split into folders like profile-pictures and event-banners
    private const string ContainerName = "raceday-images";

    private readonly BlobContainerClient _container;

    //the blob service client gets passed in from Program.cs with the connection string already set
    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _container = blobServiceClient.GetBlobContainerClient(ContainerName);
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        //makes the container the first time, blob access lets the front end show images straight from their URL
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        //new random name every upload so files never overwrite each other
        var blobName = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var blob = _container.GetBlobClient(blobName);

        //content type gets saved with the file so browsers display it instead of downloading it
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = ImageValidation.GetContentType(file.FileName) }
        });

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string fileURL)
    {
        //works out the container and file name from the URL
        var blobUri = new BlobUriBuilder(new Uri(fileURL));

        //only ever deletes from our own container
        if (blobUri.BlobContainerName != ContainerName)
        {
            return;
        }

        await _container.DeleteBlobIfExistsAsync(blobUri.BlobName);
    }
}