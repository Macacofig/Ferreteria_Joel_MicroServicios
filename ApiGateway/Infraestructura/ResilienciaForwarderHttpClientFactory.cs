using Microsoft.Extensions.Http.Resilience;
using Yarp.ReverseProxy.Forwarder;

namespace ApiGateway.Infraestructura;

/// <summary>
/// YARP no usa IHttpClientFactory, así que la resiliencia se inyecta
/// envolviendo el HttpMessageHandler saliente de cada clúster.
/// </summary>
public sealed class ResilienciaForwarderHttpClientFactory : ForwarderHttpClientFactory
{
    protected override HttpMessageHandler WrapHandler(
        ForwarderHttpClientContext context,
        HttpMessageHandler handler)
        => new ResilienceHandler(ResilienciaHttp.CrearPipelineYarp())
        {
            InnerHandler = handler
        };
}
