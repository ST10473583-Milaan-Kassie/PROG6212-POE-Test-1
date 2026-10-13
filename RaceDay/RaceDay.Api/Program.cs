var builder = WebApplication.CreateBuilder(args);

//registers the controllers so the api can find the endpoints
builder.Services.AddControllers();

var app = builder.Build();

//redirects any http requests to https
app.UseHttpsRedirection();

//maps each controller route to its endpoint
app.MapControllers();

app.Run();