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

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
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
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var roles = new[] { "Admin", "Organiser", "Participant" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

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
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }

    var participantUser = await userManager.FindByEmailAsync("manxusazekhaya@gmail.com");
    if (participantUser != null)
    {
        if (await userManager.IsInRoleAsync(participantUser, "Admin"))
        {
            await userManager.RemoveFromRoleAsync(participantUser, "Admin");
        }
        if (!await userManager.IsInRoleAsync(participantUser, "Participant"))
        {
            await userManager.AddToRoleAsync(participantUser, "Participant");
        }
    }
}

app.Run();