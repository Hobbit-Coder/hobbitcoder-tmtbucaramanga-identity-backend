namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

public abstract class BaseEntity : IBaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
}

public interface IBaseEntity
{
    public Guid Id { get; set; }
}
