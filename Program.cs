using AssignmentProg.Data;
using AssignmentProg.Models;
using AssignmentProg.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication().AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddScoped<IProfileImageService, ProfileImageService>();
builder.Services.AddSingleton<BlobStorageService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    var roles = new[] { "Admin", "Organiser", "Participant" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create role '{role}': {errors}");
            }
        }
    }

    string email = "admin@admin.com";
    string password = builder.Configuration["Seed:AdminPassword"]
        ?? throw new InvalidOperationException("Set Seed:AdminPassword in user secrets.");

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
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign Admin role: {errors}");
            }
        }
        else
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create administrator account: {errors}");
        }
    }

    var participantUser = await userManager.FindByEmailAsync("manxusazekhaya@gmail.com");
    if (participantUser != null)
    {
        if (await userManager.IsInRoleAsync(participantUser, "Admin"))
            await userManager.RemoveFromRoleAsync(participantUser, "Admin");
        if (!await userManager.IsInRoleAsync(participantUser, "Participant"))
            await userManager.AddToRoleAsync(participantUser, "Participant");
    }
}

app.Run();