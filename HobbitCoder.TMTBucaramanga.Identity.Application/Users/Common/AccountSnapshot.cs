namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

public sealed class AccountSnapshot
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }
    public required string Role { get; set; }
    public required bool IsActive { get; set; }
    public required bool CanRegisterAttendees { get; set; }
    public required string AccessStatus { get; set; }
}
