using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartAttendanceSystem;
using SmartAttendanceSystem.Services;
using SmartAttendanceSystem.Properties.Model;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IFaceImageStorage, LocalFaceImageStorage>();
}
else
{
    var blobServiceUriValue = builder.Configuration["FaceStorage:BlobServiceUri"];
    if (!Uri.TryCreate(blobServiceUriValue, UriKind.Absolute, out var blobServiceUri) ||
        blobServiceUri.Scheme != Uri.UriSchemeHttps)
    {
        throw new InvalidOperationException(
            "Configure FaceStorage:BlobServiceUri with the HTTPS URL of the Azure Storage account.");
    }

    var containerName = builder.Configuration["FaceStorage:ContainerName"] ?? "face-images";
    builder.Services.AddSingleton(new BlobServiceClient(blobServiceUri, new DefaultAzureCredential()));
    builder.Services.AddSingleton(provider =>
        provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(containerName));
    builder.Services.AddSingleton<IFaceImageStorage, AzureBlobFaceImageStorage>();
}

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
    options.Password.RequiredLength = builder.Environment.IsDevelopment() ? 6 : 12;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = !builder.Environment.IsDevelopment();
    options.Password.RequireUppercase = !builder.Environment.IsDevelopment();
    options.Password.RequiredUniqueChars = builder.Environment.IsDevelopment() ? 1 : 4;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var app = builder.Build();

var adminUsername = builder.Configuration["AdminBootstrap:Username"];
var adminEmail = builder.Configuration["AdminBootstrap:Email"];
var adminPassword = builder.Configuration["AdminBootstrap:Password"];

if (!app.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(adminUsername) ||
     string.IsNullOrWhiteSpace(adminEmail) ||
     string.IsNullOrWhiteSpace(adminPassword)))
{
    throw new InvalidOperationException(
        "Configure AdminBootstrap:Username, AdminBootstrap:Email, and AdminBootstrap:Password in deployed environments.");
}

if (app.Environment.IsDevelopment())
{
    adminUsername ??= "admin";
    adminEmail ??= "admin@example.com";
    adminPassword ??= "Admin123!";
}

var bootstrapUsername = adminUsername!;
var bootstrapEmail = adminEmail!;
var bootstrapPassword = adminPassword!;

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
    var existing = await userManager.FindByNameAsync(bootstrapUsername);
    if (existing == null)
    {
        existing = new ApplicationUser
        {
            UserName = bootstrapUsername,
            Email = bootstrapEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(existing, bootstrapPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the configured admin user: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
    else
    {
        if (!app.Environment.IsDevelopment() && !await userManager.CheckPasswordAsync(existing, bootstrapPassword))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(existing);
            var resetResult = await userManager.ResetPasswordAsync(existing, resetToken, bootstrapPassword);
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
