namespace BuildingBlocks.Application.Results;

/// <summary>
/// Erro de validação de entrada, com as mensagens agrupadas por campo.
/// Produzido pelo ValidationDecorator a partir do FluentValidation.
/// </summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation);
