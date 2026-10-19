using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Audit.Common;

public class AccountAuditEntryResponse
{
    public required Guid Id { get; set; }
    public required DateTime OccurredAt { get; set; }
    public required string ActorUserId { get; set; }
    public required AccountAuditAction Action { get; set; }
    public required Guid TargetUserId { get; set; }
    public required string TargetName { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
}
