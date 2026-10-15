using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests.Helpers;

//makes a fresh in memory database for each test so no test can mess with another tests data
public static class TestDbContextFactory
{
    public static RaceDayDbContext Create()
    {
        //a new random name every time means a brand new database every time
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RaceDayDbContext(options);

        //builds the tables from the dbcontext and loads the rows from SeedData.cs
        context.Database.EnsureCreated();

        return context;
    }
}