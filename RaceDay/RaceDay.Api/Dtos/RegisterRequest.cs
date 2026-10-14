using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

//what the client sends to register, matches the request body in the endpoint plan
public class RegisterRequest
{
    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    //nullable so leaving it out fails [Required] instead of quietly becoming 0001-01-01
    [Required]
    public DateOnly? DateOfBirth { get; set; }

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    //min 8 so passwords arent too weak, max 72 bcs bcrypt ignores anything past 72
    [Required, MinLength(8), MaxLength(72)]
    public string Password { get; set; } = string.Empty;

    //either organiser or participant, checked in the controller
    [Required]
    public string RoleName { get; set; } = string.Empty;
}