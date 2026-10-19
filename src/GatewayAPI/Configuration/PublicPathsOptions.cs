namespace GatewayAPI.Configuration;

/// <summary>
/// Список публичных маршрутов Gateway, которые НЕ требуют авторизации.
/// Всё, что не попало в этот список, считается защищённым и требует
/// валидного JWT-токена.
/// </summary>
public class PublicPathsOptions
{
    public const string SectionName = "PublicPaths";

    public string[] Paths { get; set; } = Array.Empty<string>();
}
