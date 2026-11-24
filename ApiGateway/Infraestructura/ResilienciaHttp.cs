using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace ApiGateway.Infraestructura;

/// <summary>
/// Pipelines de resiliencia acotados: pocos reintentos cortos y un timeout
/// total, para no amplificar la carga cuando un destino está caído.
/// </summary>
public static class ResilienciaHttp
{
    public static ResiliencePipeline<HttpResponseMessage> CrearPipelineYarp() => CrearPipeline(
        total: TimeSpan.FromSeconds(12),
        porIntento: TimeSpan.FromSeconds(5));

    public static ResiliencePipeline<HttpResponseMessage> CrearPipelineKeycloak() => CrearPipeline(
        total: TimeSpan.FromSeconds(6),
        porIntento: TimeSpan.FromSeconds(3));

    private static ResiliencePipeline<HttpResponseMessage> CrearPipeline(TimeSpan total, TimeSpan porIntento)
    {
        var reintento = new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Linear,
            UseJitter = true
        };

        // Nunca repetir escrituras: evita duplicar altas, ediciones o borrados.
        reintento.DisableForUnsafeHttpMethods();

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(total)
            .AddRetry(reintento)
            .AddTimeout(porIntento)
            .Build();
    }
}
