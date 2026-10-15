namespace RaceDay.Api.Helpers;

//shortcut for getting the logged in user's ID out of the session
//only used on endpoints with [SessionAuthorize], the filter already checked the ID is there
public static class CurrentUserExtensions
{
    public static int GetCurrentUserID(this ISession session)
    {
        //should never be null here, if it is something is wrong with how the endpoint is set up
        return session.GetInt32(SessionKeys.UserID)
            ?? throw new InvalidOperationException("No logged in user in the session, check the endpoint has [SessionAuthorize].");
    }
}