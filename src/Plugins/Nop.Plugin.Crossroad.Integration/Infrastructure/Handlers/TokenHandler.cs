using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Core.Infrastructure;
using Nop.Plugin.Crossroad.Integration.Services.Onix;

namespace Nop.Plugin.Crossroad.Integration.Infrastructure.Handlers;

public class TokenHandler : DelegatingHandler
{
    private readonly IStaticCacheManager _staticCacheManager;

    public TokenHandler(IStaticCacheManager staticCacheManager) => _staticCacheManager = staticCacheManager;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenValidationUnixDateTime = await _staticCacheManager.GetAsync(_staticCacheManager
                                                                        .PrepareKeyForDefaultCache(IntegrationDefaults.TokenExpirationCacheKey),
                                                                 () => default(int));

        DateTime? tokenValidationPeriod = DateTimeOffset.FromUnixTimeSeconds(tokenValidationUnixDateTime).DateTime;

        if (tokenValidationPeriod < DateTime.UtcNow)
        {
            var onixLoginService = EngineContext.Current.Resolve<OnixLoginService>();
            
            await onixLoginService.GetTokenAsync();
        }

        var token = await _staticCacheManager.GetAsync(_staticCacheManager
                .PrepareKeyForDefaultCache(IntegrationDefaults.AccessTokenCacheKey),
            () => string.Empty);

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}