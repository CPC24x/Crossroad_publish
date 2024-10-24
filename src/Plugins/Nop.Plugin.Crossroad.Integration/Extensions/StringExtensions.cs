using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nop.Plugin.Crossroad.Integration.Extensions;

public static class StringExtensions
{
    public static string[] StringToArray(this string value)
    {
        string[] values = Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(value))
        {
            values = value.Split(new[] { ';', '/', ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        return values;
    }

    public static string ChangeUnderscoreWithWhitespace(this string value) => value.Replace("_", string.Empty).TrimStart().TrimEnd();

    public static async Task<byte[]> GetBytesFromUrlAsync(this string url)
    {
        using HttpClient client = new ();

        using var response = await client.GetAsync(url);

        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }
}