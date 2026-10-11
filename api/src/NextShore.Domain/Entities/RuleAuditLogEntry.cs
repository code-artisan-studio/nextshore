using NextShore.Domain.Enums;

namespace NextShore.Domain.Entities;

public class RuleAuditLogEntry
{
    public Guid Id { get; set; }

    /// <summary>Rule table name, e.g. <c>requirements</c>.</summary>
    public required string Entity { get; set; }
    public Guid EntityId { get; set; }
    public AuditAction Action { get; set; }

    /// <summary>Row snapshots as raw JSON.</summary>
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public required string Actor { get; set; }
    public DateTimeOffset At { get; set; }
}
