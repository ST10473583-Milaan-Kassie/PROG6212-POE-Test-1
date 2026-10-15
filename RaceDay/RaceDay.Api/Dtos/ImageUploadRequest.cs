using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

//the image sent for a profile picture or event banner, sent as multipart/form-data
//wrapped in a class so swagger shows a file picker for it
public class ImageUploadRequest
{
    //the type, size and contents get checked by ImageValidation
    [Required]
    public IFormFile? File { get; set; }
}