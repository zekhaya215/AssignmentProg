using System.Security.Claims;
using AssignmentProg.Data;
using AssignmentProg.Models;
using AssignmentProg.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentProg.Controllers.Api;

[ApiController, Route("api/enrolments")]
[Authorize(Roles = "Participant", AuthenticationSchemes = "Bearer")]
public class EnrolmentsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public EnrolmentsApiController(ApplicationDbContext db) => _db = db;
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    public async Task<IActionResult> Enrol(EnrolDto dto)
    {
        var ev = await _db.Events.FindAsync(dto.EventId);
        if (ev == null) return NotFound("Event not found.");
        if (ev.Date < DateTime.Now) return BadRequest("This event has already taken place.");
        if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.EventId == dto.EventId))
            return BadRequest("Invalid category for this event.");
        if (await _db.Enrolments.AnyAsync(e => e.EventId == dto.EventId && e.ParticipantId == Uid))
            return BadRequest("You are already enrolled in this event.");

        _db.Enrolments.Add(new Enrolment { EventId = dto.EventId, CategoryId = dto.CategoryId, ParticipantId = Uid });
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine() =>
        Ok(await _db.Enrolments.Where(e => e.ParticipantId == Uid).Select(Maps.ToOut).ToListAsync());
}

[ApiController, Route("api/results")]
[Authorize(Roles = "Participant", AuthenticationSchemes = "Bearer")]
public class ResultsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ResultsApiController(ApplicationDbContext db) => _db = db;

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var rows = await _db.Results
            .Where(r => r.Enrolment!.ParticipantId == uid)
            .Select(r => new
            {
                r.FinishTime,
                r.Position,
                EventName = r.Enrolment!.Event!.Name,
                EventDate = r.Enrolment.Event.Date,
                Category = r.Enrolment.Category!.Name,
                Total = _db.Results.Count(x => x.Enrolment!.EventId == r.Enrolment.EventId)
            }).ToListAsync();

        return Ok(rows.Select(r => new ResultOut(r.EventName, r.EventDate, r.Category,
            r.FinishTime.ToString(@"hh\:mm\:ss"), r.Position, r.Total)));
    }
}

[ApiController, Route("api/profile")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class ProfileApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _um;
    private readonly BlobStorageService _blob;
    public ProfileApiController(UserManager<ApplicationUser> um, BlobStorageService blob) { _um = um; _blob = blob; }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var u = await _um.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return u == null ? NotFound() : Ok(new
        {
            name = u.FirstName + " " + u.LastName,
            email = u.Email,
            phone = u.PhoneNumber,
            profilePictureUrl = u.ProfileImagePath
        });
    }

    [HttpPut, Consumes("multipart/form-data")]
    public async Task<IActionResult> Update([FromForm] ProfileUpdate dto)
    {
        var u = await _um.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (u == null) return NotFound();

        var parts = dto.Name.Trim().Split(' ', 2);
        u.FirstName = parts[0];
        u.LastName = parts.Length > 1 ? parts[1] : "-";
        u.PhoneNumber = dto.Phone;

        if (dto.Picture is { Length: > 0 })
        {
            var url = await _blob.UploadAsync(dto.Picture, "profiles");
            if (url == null) return BadRequest("Picture must be a jpg, png or webp under 5 MB.");
            u.ProfileImagePath = url;
        }
        await _um.UpdateAsync(u);
        return Ok();
    }
}