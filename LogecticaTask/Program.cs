using DocumentFormat.OpenXml.InkML;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging; // Add this namespace
using System.Threading;
using WebUI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure your database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddTransient<DbSeeder>(); // Add this line to register DbSeeder

var app = builder.Build();

// Scope for dependency injection to resolve services
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;


    using var context = services.GetRequiredService<ApplicationDbContext>();

    // Resolve ILogger<DbSeeder>
    var logger = services.GetRequiredService<ILogger<DbSeeder>>();

    // Resolve IProductRepository
    var productRepository = services.GetRequiredService<IProductRepository>();


    // Create an instance of DbSeeder
    var dbSeeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    // Seed the database with the resolved dependencies
    try
    {
        await dbSeeder.SeedAsync(context,productRepository, CancellationToken.None);
    }
    catch (Exception ex)
    {
        // Log any errors during seeding
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("An error occurred.");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

app.Run();
