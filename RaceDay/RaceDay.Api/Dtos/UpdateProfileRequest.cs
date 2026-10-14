using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

//what the client sends to update their profile, matches the request body in the endpoint plan
//no dateofbirth or email on purpose, neither can be changed through the profile
public class UpdateProfileRequest
{
    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    //optional, digits and spaces with an optional + in front, null or empty clears it
    [MaxLength(20)]
    [RegularExpression(@"^\+?[0-9 ]+$", ErrorMessage = "Phone number may only contain digits, spaces and an optional leading +.")]
    public string? PhoneNumber { get; set; }
}