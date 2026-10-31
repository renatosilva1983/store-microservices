namespace Catalog.Domain.Common;

/// <summary>
/// Entidade (DDD): tem identidade. Duas entidades são iguais se têm o mesmo Id,
/// mesmo que os outros dados sejam diferentes.
/// </summary>
public abstract class Entity
{
    protected Entity(Guid id)
    {
        Id = id;
    }

    // Construtor usado pelo EF Core ao materializar a entidade do banco.
    protected Entity()
    {
    }

    public Guid Id { get; private set; }

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
