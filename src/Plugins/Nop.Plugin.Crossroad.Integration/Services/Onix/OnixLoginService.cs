using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Plugin.Crossroad.Integration.Domains.Onix;
using Nop.Services.Configuration;

namespace Nop.Plugin.Crossroad.Integration.Services.Onix;

public class OnixLoginService
{
    private readonly HttpClient _client;
    private readonly IStaticCacheManager _cacheManager;
    private readonly ISettingService _settingService;

    public OnixLoginService(HttpClient client,
                            IStaticCacheManager cacheManager,
                            ISettingService settingService)
    {
        _client = client;
        _cacheManager = cacheManager;
        _settingService = settingService;
    }

    public async Task GetTokenAsync()
    {
        var onixEditSettings = await _settingService.LoadSettingAsync<OnixEditSettings>();

        var request = new Contracts.LoginRequest(onixEditSettings.Username, onixEditSettings.Password);

        HttpResponseMessage apiResult = await _client.PostAsJsonAsync("login", request);

        var error = await apiResult.Content.ReadAsStringAsync();

        apiResult.EnsureSuccessStatusCode();

        var response = await apiResult.Content.ReadAsStringAsync();

        Contracts.LoginResponse? deserilizedResponse = JsonSerializer.Deserialize<Contracts.LoginResponse>(response);

        await _cacheManager.SetAsync(_cacheManager.PrepareKeyForDefaultCache(IntegrationDefaults.TokenExpirationCacheKey), deserilizedResponse.Expires);

        await _cacheManager.SetAsync(_cacheManager.PrepareKeyForDefaultCache(IntegrationDefaults.AccessTokenCacheKey), deserilizedResponse.AccessToken);
    }
}