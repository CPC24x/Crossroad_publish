using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nop.Plugin.Crossroad.Integration.Services.Onix;

public class OnixEditService
{
    private readonly HttpClient _client;

    public OnixEditService(HttpClient client) => _client = client;

    private async Task<int> GetCatalogueIdAsync()
    {
        var apiResult = await _client.GetAsync("catalogue");

        apiResult.EnsureSuccessStatusCode();

        var response = await apiResult.Content.ReadAsStringAsync();

        var deserilizedResponse = JsonSerializer.Deserialize<List<Contracts.CatalogueResponse>>(response);

        return deserilizedResponse.Any() ? deserilizedResponse.FirstOrDefault()!.Id : throw new ArgumentOutOfRangeException();
    }
    
    public async Task<List<Contracts.CatalogueProductsResponse>> GetOnixProductsAsync()
    {
        var catalogueId = await GetCatalogueIdAsync();

        var apiResult = await _client.GetAsync($"3.0/product/{catalogueId}");

        apiResult.EnsureSuccessStatusCode();

        var response = await apiResult.Content.ReadAsStringAsync();

        var deserializeResponse = JsonSerializer.Deserialize<List<Contracts.CatalogueProductsResponse>>(response);

        return deserializeResponse;
    }
}