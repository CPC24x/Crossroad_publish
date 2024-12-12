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

    public async Task<List<Contracts.CatalogueProductsResponse>> GetOnixProductsAsync(int page = 0, int pageSize = int.MaxValue, string isbn = "")
    {
        var catalogueId = await GetCatalogueIdAsync();

        var searchCriteria = new SearchCriteria
        {
            orderByCol = "ISBN13",
            onixVersion = "ONIX3",
            whereConditionsOperator = "AND",
        };

        var urlEncodedJson = string.Empty;
        if (!string.IsNullOrWhiteSpace(isbn))
        {
            searchCriteria.whereConditions.Add(new WhereConditions
            {
                columnName = "ISBN13",
                conditionOperator = "In",
                value = isbn,
            });

            urlEncodedJson = Uri.EscapeDataString(JsonSerializer.Serialize(searchCriteria));
        }

        var apiResult = await _client.GetAsync($"3.0/product/{catalogueId}?page={page}&pageSize={pageSize}&searchCriteria={urlEncodedJson}");

        apiResult.EnsureSuccessStatusCode();

        var response = await apiResult.Content.ReadAsStringAsync();

        var deserializeResponse = JsonSerializer.Deserialize<List<Contracts.CatalogueProductsResponse>>(response);

        return deserializeResponse;
    }

    public class SearchCriteria
    {
        public SearchCriteria()
        {
            whereConditions = new List<WhereConditions>();
        }

        public string orderByCol { get; set; }
        public string onixVersion { get; set; }
        public IList<WhereConditions> whereConditions { get; set; }
        public string whereConditionsOperator { get; set; }

    }

    public class WhereConditions
    {
        public string columnName { get; set; }
        public string conditionOperator { get; set; }
        public string value { get; set; }
    }
}