using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

//what gets sent back about a user, leaves out the password hash
//reused by register, login and the profile endpoints
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

    //builds the response from a user so its only written once
    //rolename gets passed in so the role doesnt always have to be loaded from the database
    public static UserResponse FromUser(User user, string roleName)
    {
        return new UserResponse
        {
            UserID = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RoleName = roleName,
            ProfileImageURL = user.ProfileImageURL
        };
    }
}