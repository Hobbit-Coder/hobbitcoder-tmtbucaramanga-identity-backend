namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
    public string CreatedBy { get; set; } = Guid.Empty.ToString();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public interface IAuditableEntity
{
    public string CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}