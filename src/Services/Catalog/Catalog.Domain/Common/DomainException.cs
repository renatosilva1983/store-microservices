namespace Catalog.Domain.Common;

/// <summary>
/// Violação de uma invariante do domínio (regra que nunca pode ser quebrada).
/// A validação de entrada na Application impede que isso aconteça no fluxo normal;
/// se acontecer, é um erro de programação ou de chamada, e a API devolve 400.
/// O Domain não depende de nenhum outro projeto, por isso não usa o Result do BuildingBlocks.
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
