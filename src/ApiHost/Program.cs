using Users.Application;
using Users.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// Configure database connection
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                         throw new Exception("No connection string found.");

// Register Users module services
builder.Services.AddUsersInfrastructureServices(connectionString);
builder.Host.AddUsersApplicationServices();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
