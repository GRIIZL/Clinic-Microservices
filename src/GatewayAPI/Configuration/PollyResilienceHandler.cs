using Polly;
using Polly.Extensions.Http;

namespace GatewayAPI.Configuration;

/// <summary>
/// Polly-политики для Ocelot: retry, circuit breaker, timeout.
/// Настраиваются из секции "PollySettings" в appsettings.json.
/// </summary>
public class PollyResilienceHandler : DelegatingHandler
{
    private readonly IAsyncPolicy<HttpResponseMessage> _executeAsync;

    public PollyResilienceHandler(IConfiguration configuration)
    {
        var section = configuration.GetSection("PollySettings");
        int retryCount = section.GetValue("RetryCount", 3);
        int retryDelayMs = section.GetValue("RetryDelayMs", 500);
        int breakerBreakDurationSec = section.GetValue("CircuitBreakerDurationOfBreakSec", 30);
        int breakerSamplingDurationSec = section.GetValue("CircuitBreakerSamplingDurationSec", 30);
        int breakerMinThroughput = section.GetValue("CircuitBreakerMinimumThroughput", 8);
        int requestTimeoutSec = section.GetValue("RequestTimeoutSec", 15);

        // 1. Timeout — общий лимит на запрос
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(requestTimeoutSec));

        // 2. Retry с экспоненциальной задержкой
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount,
                attempt => TimeSpan.FromMilliseconds(retryDelayMs * Math.Pow(2, attempt - 1)));

        // 3. Advanced Circuit Breaker (50% ошибок за 30 сек → break на 30 сек)
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5,
                samplingDuration: TimeSpan.FromSeconds(breakerSamplingDurationSec),
                minimumThroughput: breakerMinThroughput,
                durationOfBreak: TimeSpan.FromSeconds(breakerBreakDurationSec),
                onBreak: (_, breakDuration) =>
                {
                    Console.WriteLine($"[Polly] Circuit OPENED. Break: {breakDuration.TotalSeconds}s");
                },
                onReset: () =>
                {
                    Console.WriteLine("[Polly] Circuit RESET.");
                });

        // Полики применяются в порядке: timeout → retry → circuit breaker
        _executeAsync = Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _executeAsync.ExecuteAsync(
            ct => base.SendAsync(request, ct), cancellationToken);
    }
}
