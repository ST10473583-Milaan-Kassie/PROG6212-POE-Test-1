using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs.Responses;

//what gets sent back about a category
public class CategoryResponse
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    //null means the category is open to every age
    public int? MinAge { get; set; }

    //null means there is no maximum age
    public int? MaxAge { get; set; }

    //in km, null means its an age only category
    public decimal? Distance { get; set; }

    //builds the response from a category so its only written once
    public static CategoryResponse FromCategory(Category category)
    {
        return new CategoryResponse
        {
            CategoryID = category.CategoryID,
            EventID = category.EventID,
            CategoryName = category.CategoryName,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            Distance = category.Distance
        };
    }
}