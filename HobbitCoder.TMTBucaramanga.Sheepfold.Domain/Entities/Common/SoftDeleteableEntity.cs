namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

public abstract class SoftDeleteableEntity : AuditableEntity, ISoftDeleteableEntity
{
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public interface ISoftDeleteableEntity : IAuditableEntity
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
