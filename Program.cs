using AssignmentProg.Data;
using AssignmentProg.Models;
using AssignmentProg.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== services =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Profile image service
builder.Services.AddScoped<IProfileImageService, ProfileImageService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ===== pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ===== seeding block =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // Create application roles
    var roles = new[] { "Admin", "Organiser", "Participant" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Failed to create role '{role}': {errors}");
            }
        }
    }

    // Create default administrator account
    string email = "admin@admin.com";
    string password = "Zekhaya215@";

    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User",
            Gender = Gender.Male
        };

        var createResult = await userManager.CreateAsync(user, password);

        if (createResult.Succeeded)
        {
            var roleResult = await userManager.AddToRoleAsync(user, "Admin");

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Failed to assign Admin role: {errors}");
            }
        }
        else
        {
            var errors = string.Join(
                "; ",
                createResult.Errors.Select(e => e.Description));

            throw new InvalidOperationException(
                $"Failed to create administrator account: {errors}");
        }
    }

    // Ensure the specified user has the Participant role
    var participantUser = await userManager.FindByEmailAsync(
        "manxusazekhaya@gmail.com");

    if (participantUser != null)
    {
        if (await userManager.IsInRoleAsync(participantUser, "Admin"))
        {
            await userManager.RemoveFromRoleAsync(
                participantUser, "Admin");
        }

        if (!await userManager.IsInRoleAsync(
                participantUser, "Participant"))
        {
            await userManager.AddToRoleAsync(
                participantUser, "Participant");
        }
    }
}

app.Run();