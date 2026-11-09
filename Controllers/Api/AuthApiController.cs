using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssignmentProg.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AssignmentProg.Controllers.Api;

[ApiController, Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _um;
    private readonly IConfiguration _cfg;
    public AuthApiController(UserManager<ApplicationUser> um, IConfiguration cfg) { _um = um; _cfg = cfg; }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (dto.Role != "Organiser" && dto.Role != "Participant")
            return BadRequest("Role must be Organiser or Participant.");

        var parts = dto.Name.Trim().Split(' ', 2);
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true,
            FirstName = parts[0],
            LastName = parts.Length > 1 ? parts[1] : "-",
            Gender = Gender.PreferNotToSay
        };
        var res = await _um.CreateAsync(user, dto.Password);
        if (!res.Succeeded) return BadRequest(string.Join(" ", res.Errors.Select(e => e.Description)));

        await _um.AddToRoleAsync(user, dto.Role);
        return Ok(new { message = "Registered" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _um.FindByEmailAsync(dto.Email);
        if (user == null || !await _um.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("Invalid email or password.");

        var role = (await _um.GetRolesAsync(user)).FirstOrDefault() ?? "Participant";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var jwt = new JwtSecurityToken(_cfg["Jwt:Issuer"], _cfg["Jwt:Audience"], claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(jwt), userId = user.Id, role, name = user.FullName });
    }
}