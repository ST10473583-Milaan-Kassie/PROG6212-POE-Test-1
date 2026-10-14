using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

//what gets sent back about a category
public class CategoryResponse
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int MinAge { get; set; }

    //null means there is no maximum age
    public int? MaxAge { get; set; }

    //builds the response from a category so its only written once
    public static CategoryResponse FromCategory(Category category)
    {
        return new CategoryResponse
        {
            CategoryID = category.CategoryID,
            EventID = category.EventID,
            CategoryName = category.CategoryName,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge
        };
    }
}