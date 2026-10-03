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

// Register AppDbContext (reads DefaultConnection from appsettings.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Apply pending migrations and seed a default admin user for development (username: admin)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    // Use the existing database (migrations were applied during development). If you want automatic migration at startup
    // re-enable db.Database.Migrate(), but be aware of pending-model-change checks.
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    // Ensure Admin and Teacher roles exist
    var roles = new[] { "Admin", "Teacher", "Student" };
    foreach (var r in roles)
    {
        var exists = roleManager.RoleExistsAsync(r).GetAwaiter().GetResult();
        if (!exists)
        {
            roleManager.CreateAsync(new IdentityRole(r)).GetAwaiter().GetResult();
        }
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var existing = userManager.FindByNameAsync("admin").GetAwaiter().GetResult();
    if (existing == null)
    {
        var admin = new ApplicationUser { UserName = "admin", Email = "admin@example.com", EmailConfirmed = true };
        var result = userManager.CreateAsync(admin, "Admin123!").GetAwaiter().GetResult();
        if (result.Succeeded)
        {
            userManager.AddToRoleAsync(admin, "Admin").GetAwaiter().GetResult();
        }
    }
    else
    {
        if (!userManager.IsInRoleAsync(existing, "Admin").GetAwaiter().GetResult())
        {
            userManager.AddToRoleAsync(existing, "Admin").GetAwaiter().GetResult();
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
