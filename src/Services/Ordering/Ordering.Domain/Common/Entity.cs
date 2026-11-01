namespace Ordering.Domain.Common;

/// <summary>
/// Entidade (DDD): igualdade pela identidade.
/// Cada serviço tem a sua base — bounded contexts não compartilham código de domínio.
/// </summary>
public abstract class Entity
{
    protected Entity(Guid id)
    {
        Id = id;
    }

    // EF Core
    protected Entity()
    {
    }

    public Guid Id { get; private set; }

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
