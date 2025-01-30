using AspNetCoreRateLimit;
using log4net;
using Microsoft.Extensions.Options;

namespace Utils.Other
{
    public class CustomIpRateLimitMiddleware : IpRateLimitMiddleware
    {
        private static ILog log = LogManager.GetLogger(typeof(CustomIpRateLimitMiddleware));

        public CustomIpRateLimitMiddleware(RequestDelegate next
            , IProcessingStrategy processingStrategy
            , IOptions<IpRateLimitOptions> options
            , IRateLimitCounterStore counterStore
            , IIpPolicyStore policyStore
            , IRateLimitConfiguration config
            , ILogger<IpRateLimitMiddleware> logger)
                : base(next, processingStrategy, options, policyStore, config, logger)
        {
            log.Debug("Initialize CustomIpRateLimitMiddleware");
        }

        protected override void LogBlockedRequest(HttpContext httpContext, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule)
        {
            log.Warn($"Request {identity.HttpVerb}:{identity.Path} from IP {identity.ClientIp} has been blocked, quota {rule.Limit}/{rule.Period} exceeded by {counter.Count - rule.Limit}. Blocked by rule {rule.Endpoint}, TraceIdentifier {httpContext.TraceIdentifier}. MonitorMode: {rule.MonitorMode}");
        }
    }
    public class CustomRateLimitConfiguration : RateLimitConfiguration
    {
        private ILog log = LogManager.GetLogger(typeof(CustomRateLimitConfiguration));

        public CustomRateLimitConfiguration(
            IOptions<IpRateLimitOptions> ipOptions,
            IOptions<ClientRateLimitOptions> clientOptions)
                : base(ipOptions, clientOptions)
        {
            log.Debug("Initialize CustomRateLimitConfiguration");
        }

        public override void RegisterResolvers()
        {
            base.RegisterResolvers();
            log.Debug("RegisterResolvers in CustomRateLimitConfiguration");
            ClientResolvers.Add(new ClientIPPortResolveContributor());
        }
    }

    public class ClientIPPortResolveContributor : IClientResolveContributor
    {
        private ILog log = LogManager.GetLogger(typeof(ClientIPPortResolveContributor));

        public Task<string> ResolveClientAsync(HttpContext httpContext)
        {
            var idpaddr = httpContext.Connection.RemoteIpAddress;
            var port = httpContext.Connection.RemotePort;

            string result = idpaddr + ":" + Convert.ToString(port);

            log.Debug("ResolveClientAsync(): " + result);
            return Task.FromResult(result);
        }
    }
}
