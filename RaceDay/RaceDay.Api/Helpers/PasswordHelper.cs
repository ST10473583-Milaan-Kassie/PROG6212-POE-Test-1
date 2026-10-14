namespace RaceDay.Api.Helpers;

//hashes and checks passwords with bcrypt so the actual password is never stored
public static class PasswordHelper
{
    //work factor 11 matches the seeded hashes ($2b$11$...)
    private const int WorkFactor = 11;

    //turns the plain password into a salted hash for storing, the salt is kept inside the hash
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    //checks a login attempt against the stored hash
    public static bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}