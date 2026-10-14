namespace RaceDay.Api.Models;

//a user is either an organiser or participant
//the role dictates what they can do(which api endpoints they can use)
public class User
{
    //PK
    public int UserID { get; set; }

    //FK, which role this user has(participant or organiser)
    public int RoleID { get; set; }

    //both required
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    //dateonly maps to DATE in sql, used to check the category age restrictions when enrolling
    public DateOnly DateOfBirth { get; set; }

    //used to log in, must be unique
    public string Email { get; set; } = string.Empty;

    //nullable bcs its not required at registration
    public string? PhoneNumber { get; set; }

    //only the hash gets stored, never the actual password
    public string PasswordHash { get; set; } = string.Empty;

    //null until they upload a pic
    public string? ProfileImageURL { get; set; }

    //the role this user belongs to, ef fills this in
    public Role Role { get; set; } = null!;

    //the events this user has created, only organisers will have any
    public ICollection<Event> Events { get; set; } = new List<Event>();

    //the events this user has enrolled in, only participants will have any
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}