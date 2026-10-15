namespace RaceDay.Api.Services;

//stores uploaded files, the API only talks to this interface so whats behind it can change
//azurite for now, real azure blob storage in part 3, and a fake one in the unit tests
public interface IFileStorageService
{
    //saves the file in the given folder and returns the URL its reachable at
    Task<string> UploadAsync(IFormFile file, string folder);

    //removes a file using the URL uploadasync gave back, does nothing if its already gone
    Task DeleteAsync(string fileURL);
}