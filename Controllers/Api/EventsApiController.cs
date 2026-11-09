using System.Globalization;
using System.Security.Claims;
using AssignmentProg.Data;
using AssignmentProg.Models;
using AssignmentProg.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentProg.Controllers.Api;

[ApiController, Route("api/events")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class EventsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly BlobStorageService _blob;
    public EventsApiController(ApplicationDbContext db, BlobStorageService blob) { _db = db; _blob = blob; }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static IQueryable<EventOut> Project(IQueryable<Event> q) => q.Select(e => new EventOut(
        e.Id, e.Name, e.Description, e.Date, e.Location, e.DistanceKm, e.EventType,
        e.BannerImageUrl, e.Enrolments.Count));

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> All() =>
        Ok(await Project(_db.Events).OrderBy(e => e.Date).ToListAsync());

    [Authorize(Roles = "Organiser", AuthenticationSchemes = "Bearer"), HttpGet("mine")]
    public async Task<IActionResult> Mine() =>
        Ok(await Project(_db.Events.Where(e => e.OrganiserId == Uid)).OrderBy(e => e.Date).ToListAsync());

    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<IActionResult> One(int id)
    {
        var e = await Project(_db.Events.Where(x => x.Id == id)).FirstOrDefaultAsync();
        return e == null ? NotFound() : Ok(e);
    }

    [Authorize(Roles = "Organiser", AuthenticationSchemes = "Bearer")]
    [HttpPost, Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] EventUpsert dto)
    {
        var ev = new Event { OrganiserId = Uid };
        if (!TryApply(ev, dto, out var error)) return BadRequest(error);

        if (dto.Banner is { Length: > 0 })
        {
            ev.BannerImageUrl = await _blob.UploadAsync(dto.Banner, "banners");
            if (ev.BannerImageUrl == null) return BadRequest("Banner must be a jpg, png or webp under 5 MB.");
        }
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return Ok(new { ev.Id });
    }

    [Authorize(Roles = "Organiser", AuthenticationSchemes = "Bearer")]
    [HttpPut("{id:int}"), Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] EventUpsert dto)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganiserId == Uid);
        if (ev == null) return NotFound();
        if (!TryApply(ev, dto, out var error)) return BadRequest(error);

        if (dto.Banner is { Length: > 0 })
        {
            var url = await _blob.UploadAsync(dto.Banner, "banners");
            if (url == null) return BadRequest("Banner must be a jpg, png or webp under 5 MB.");
            ev.BannerImageUrl = url;
        }
        await _db.SaveChangesAsync();
        return Ok(new { ev.Id });
    }

    [Authorize(Roles = "Organiser", AuthenticationSchemes = "Bearer")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganiserId == Uid);
        if (ev == null) return NotFound();
        if (await _db.Enrolments.AnyAsync(x => x.EventId == id))
            return BadRequest("This event has enrolments and cannot be deleted.");
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [AllowAnonymous, HttpGet("{id:int}/categories")]
    public async Task<IActionResult> Categories(int id) =>
        Ok(await _db.Categories.Where(c => c.EventId == id)
            .Select(c => new CategoryOut(c.Id, c.Name, c.Kind)).ToListAsync());

    [Authorize(Roles = "Organiser", AuthenticationSchemes = "Bearer")]
    [HttpPost("{id:int}/categories")]
    public async Task<IActionResult> AddCategory(int id, NewCategory dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Category name is required.");
        if (!await _db.Events.AnyAsync(e => e.Id == id && e.OrganiserId == Uid)) return NotFound();

        var kind = Enum.TryParse<CategoryKind>(dto.Kind, true, out var k) ? k : CategoryKind.Age;
        var c = new Category { EventId = id, Name = dto.Name.Trim(), Kind = kind };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return Ok(new CategoryOut(c.Id, c.Name, c.Kind));
    }

    [Authorize(Roles = "Organiser", AuthenticationSchemes = "Bearer")]
    [HttpGet("{id:int}/enrolments")]
    public async Task<IActionResult> Enrolments(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == id && e.OrganiserId == Uid)) return NotFound();
        return Ok(await _db.Enrolments.Where(x => x.EventId == id).Select(Maps.ToOut).ToListAsync());
    }

    [Authorize(Roles = "Organiser", AuthenticationSchemes = "Bearer")]
    [HttpPost("{id:int}/results")]
    public async Task<IActionResult> PublishResults(int id, List<ResultEntryDto> entries)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganiserId == Uid);
        if (ev == null) return NotFound();
        if (ev.Date > DateTime.Now) return BadRequest("The event has not concluded yet.");
        if (entries.Count == 0) return BadRequest("Enter at least one result.");

        foreach (var en in entries)
        {
            if (!TimeSpan.TryParse(en.FinishTime, CultureInfo.InvariantCulture, out var time))
                return BadRequest($"Invalid finish time: {en.FinishTime}. Use hh:mm:ss.");
            if (en.Position < 1) return BadRequest("Position must be 1 or higher.");

            var enrolment = await _db.Enrolments.FirstOrDefaultAsync(x => x.Id == en.EnrolmentId && x.EventId == id);
            if (enrolment == null) return BadRequest("Enrolment does not belong to this event.");

            var existing = await _db.Results.FirstOrDefaultAsync(r => r.EnrolmentId == enrolment.Id);
            if (existing == null)
                _db.Results.Add(new Result { EnrolmentId = enrolment.Id, FinishTime = time, Position = en.Position });
            else
            {
                existing.FinishTime = time;
                existing.Position = en.Position;
                existing.CapturedOn = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync();
        return Ok();
    }

    private static bool TryApply(Event ev, EventUpsert d, out string error)
    {
        error = "";
        if (!DateTime.TryParse(d.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        { error = "Invalid date."; return false; }
        if (!decimal.TryParse(d.DistanceKm, NumberStyles.Number, CultureInfo.InvariantCulture, out var km) || km < 0.1m || km > 1000m)
        { error = "Distance must be between 0.1 and 1000 km."; return false; }
        if (!Enum.TryParse<EventType>(d.EventType, true, out var type) || !Enum.IsDefined(type))
        { error = "Event type must be Run, Walk or Cycle."; return false; }

        ev.Name = d.Name; ev.Description = d.Description; ev.Date = date;
        ev.Location = d.Location; ev.DistanceKm = km; ev.EventType = type;
        return true;
    }
}