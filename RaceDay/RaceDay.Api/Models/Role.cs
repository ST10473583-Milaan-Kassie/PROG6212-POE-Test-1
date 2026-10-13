namespace RaceDay.Api.Models;

//lookup table for the two roles, organiser and participant
public class Role
{
    //PK, starts at 1 and goes up by 1 each time like the sql
    public int RoleID { get; set; }

    //either organiser or participant, cant be null
    public string RoleName { get; set; } = string.Empty;

    //all the users that have this role
    public ICollection<User> Users { get; set; } = new List<User>();
}