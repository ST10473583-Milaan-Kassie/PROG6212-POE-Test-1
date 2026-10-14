namespace RaceDay.Api.Dtos;

//what gets sent back about a user, leaves out the password hash
//reused by the profile endpoints later
public class UserResponse
{
    public int UserID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? ProfileImageURL { get; set; }
}