using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartAttendanceSystem;
using SmartAttendanceSystem.Properties.Model;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings:DefaultConnection before starting the application.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Use AddIdentity (not AddDefaultIdentity) - default factory adds role claims
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

var adminUsername = builder.Configuration["AdminBootstrap:Username"];
var adminEmail = builder.Configuration["AdminBootstrap:Email"];
var adminPassword = builder.Configuration["AdminBootstrap:Password"];

if (app.Environment.IsProduction() &&
    (string.IsNullOrWhiteSpace(adminUsername) ||
     string.IsNullOrWhiteSpace(adminEmail) ||
     string.IsNullOrWhiteSpace(adminPassword)))
{
    throw new InvalidOperationException(
        "Configure AdminBootstrap:Username, AdminBootstrap:Email, and AdminBootstrap:Password in production.");
}

adminUsername ??= "admin";
adminEmail ??= "admin@example.com";
adminPassword ??= "Admin123!";

await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "Teacher", "Student" };
    foreach (var r in roles)
    {
        if (!await roleManager.RoleExistsAsync(r))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(r));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create the required {r} role: {string.Join("; ", result.Errors.Select(error => error.Description))}");
            }
        }
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var existing = await userManager.FindByNameAsync(adminUsername);
    if (existing == null)
    {
        existing = new ApplicationUser
        {
            UserName = adminUsername,
            Email = adminEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(existing, adminPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the configured admin user: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
    else
    {
        if (app.Environment.IsProduction() && !await userManager.CheckPasswordAsync(existing, adminPassword))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(existing);
            var resetResult = await userManager.ResetPasswordAsync(existing, resetToken, adminPassword);
            if (!resetResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not set the configured admin password: {string.Join("; ", resetResult.Errors.Select(error => error.Description))}");
            }
        }
    }

    if (!await userManager.IsInRoleAsync(existing, "Admin"))
    {
        var result = await userManager.AddToRoleAsync(existing, "Admin");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not assign the admin role: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
