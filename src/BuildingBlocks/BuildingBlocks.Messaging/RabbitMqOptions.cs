namespace BuildingBlocks.Messaging;

/// <summary>
/// Conexão com o RabbitMQ (Options Pattern, ADR-19).
/// A senha não fica no appsettings: vem da variável de ambiente RabbitMq__Password.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";

    public ushort Port { get; init; } = 5672;

    public string VirtualHost { get; init; } = "/";

    public string Username { get; init; } = "store";

    public string Password { get; init; } = string.Empty;
}
