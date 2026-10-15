namespace RaceDay.Api.Helpers;

//works out how old someone is on a certain date
//used at registration now and for the category age check when enrolling later
public static class AgeHelper
{
    public static int GetAgeOn(DateOnly dateOfBirth, DateOnly onDate)
    {
        var age = onDate.Year - dateOfBirth.Year;

        //takes a year off if their birthday hasnt come around yet that year
        if (dateOfBirth > onDate.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    //checks if someone fits a category's age range on the event date
    //used when enrolling and when a category or event date changes, so its always worked out the same way
    public static bool IsAgeEligible(DateOnly dateOfBirth, DateOnly eventDate, int? minAge, int? maxAge)
    {
        //no min age means its a distance only category, anyone can enter
        //registration already makes sure everyone is at least 10
        if (minAge == null)
        {
            return true;
        }

        var age = GetAgeOn(dateOfBirth, eventDate);

        //no max age means anyone at or above the min age fits
        return age >= minAge.Value && (maxAge == null || age <= maxAge.Value);
    }
}