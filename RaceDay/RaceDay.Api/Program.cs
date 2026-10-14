using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;

var builder = WebApplication.CreateBuilder(args);

//registers the controllers so the api can find the endpoints
//validation errors come back in the same { message } shape as every other error, with each field's errors listed underneath
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new
            {
                message = "The request contains invalid details.",
                errors
            });
        };
    });

//registers swagger so every endpoint can be viewed and tested in the browser
builder.Services.AddSwaggerGen();

//registers the dbcontext, the connection string comes from appsettings.json
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayConnection")));

//session data gets kept in the server's memory, the browser only gets a cookie with the session id
builder.Services.AddDistributedMemoryCache();

//server side sessions, keeps the logged in user's id and role between requests
builder.Services.AddSession(options =>
{
    options.Cookie.Name = SessionKeys.CookieName;

    //javascript cant read the cookie
    options.Cookie.HttpOnly = true;

    //cookie only gets sent over https
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    //the session cookie is needed for the api to work so its always allowed
    options.Cookie.IsEssential = true;

    //logs the user out after 30 minutes of doing nothing
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

//swagger ui is only switched on while developing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//redirects any http requests to https
app.UseHttpsRedirection();

//loads the session for each request, has to come before the controllers are mapped
app.UseSession();

//maps each controller route to its endpoint
app.MapControllers();

app.Run();