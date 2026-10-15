using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

//what the client sends to log in, matches the request body in the endpoint plan
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    //no length rules here, the hash check decides if its right
    [Required]
    public string Password { get; set; } = string.Empty;
}