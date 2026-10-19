using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.AccountDirectory.Common;

public sealed class DirectoryUserResponse
{
    public required Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public required UserRole Role { get; set; }
    public required bool IsActive { get; set; }
    public required bool IsActivated { get; set; }
    public required bool CanRegisterAttendees { get; set; }
}
