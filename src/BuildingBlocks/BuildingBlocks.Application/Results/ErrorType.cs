namespace BuildingBlocks.Application.Results;

/// <summary>
/// Categoria do erro. A borda (API) usa o tipo para escolher o status HTTP,
/// sem precisar conhecer cada erro individualmente.
/// </summary>
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict
}
