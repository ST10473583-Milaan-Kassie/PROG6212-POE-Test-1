namespace RaceDay.Api.Helpers;

//the keys the logged in user gets stored under in the session
//kept in one place so login, logout and the role filter always use the same spelling
public static class SessionKeys
{
    //the ID of the logged in user
    public const string UserID = "UserID";

    //either organiser or participant
    public const string RoleName = "RoleName";
}