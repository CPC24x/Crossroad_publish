using System.Net.Http;

namespace Nop.Plugin.Crossroad.Integration.Services.OpenKm;

public class OpenKmService
{
    private readonly HttpClient _client;

    public OpenKmService(HttpClient client)
    {
        _client = client;
    }

}