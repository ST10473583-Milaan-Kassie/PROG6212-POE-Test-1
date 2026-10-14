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
}