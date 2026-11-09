using System.Linq.Expressions;
using AssignmentProg.Models;

namespace AssignmentProg.Controllers.Api;

public record RegisterDto(string Name, string Email, string Password, string Role);
public record LoginDto(string Email, string Password);

public record EventOut(int Id, string Name, string Description, DateTime Date, string Location,
    decimal DistanceKm, EventType EventType, string? BannerUrl, int EnrolmentCount);
public record CategoryOut(int Id, string Name, CategoryKind Kind);
public record NewCategory(string Name, string? Kind);
public record EnrolmentOut(int Id, int EventId, string EventName, DateTime EventDate,
    string ParticipantName, string CategoryName, string Status);
public record EnrolDto(int EventId, int CategoryId);
public record ResultOut(string EventName, DateTime EventDate, string CategoryName,
    string FinishTime, int Position, int TotalFinishers);

public class ResultEntryDto
{
    public int EnrolmentId { get; set; }
    public string? FinishTime { get; set; }
    public int Position { get; set; }
}

public class EventUpsert
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Date { get; set; } = "";
    public string Location { get; set; } = "";
    public string DistanceKm { get; set; } = "";
    public string EventType { get; set; } = "";
    public IFormFile? Banner { get; set; }
}

public class ProfileUpdate
{
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public IFormFile? Picture { get; set; }
}

public static class Maps
{
    // Status is worked out from the data: Completed (has result), Pending (event over, no result), Confirmed.
    public static readonly Expression<Func<Enrolment, EnrolmentOut>> ToOut = x => new EnrolmentOut(
        x.Id, x.EventId, x.Event!.Name, x.Event.Date,
        x.Participant!.FirstName + " " + x.Participant.LastName,
        x.Category!.Name,
        x.Result != null ? "Completed" : (x.Event.Date < DateTime.Now ? "Pending" : "Confirmed"));
}