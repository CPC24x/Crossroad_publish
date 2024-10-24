using System.Collections.Generic;
using System.Linq;
using Nop.Plugin.Crossroad.Integration.Services.Onix;

namespace Nop.Plugin.Crossroad.Integration.Infrastructure.Extensions;

public static class DictionaryExtensions
{
    public static Dictionary<string, string> ProductFromTypesToDictionary(this List<Contracts.ProductFormDetail> productFormDetails)
    {
        Dictionary<string, string> productTypes = new();

        foreach (var productFormDetail in productFormDetails.Select(pfd => pfd.ProductFormCode))
            switch (productFormDetail)
            {
                case IntegrationDefaults.Paperback:
                    productTypes.Add(IntegrationDefaults.Paperback, nameof(IntegrationDefaults.Paperback));
                    break;
                case IntegrationDefaults.Epub:
                    productTypes.Add(IntegrationDefaults.Epub, nameof(IntegrationDefaults.Epub));
                    break;
                case IntegrationDefaults.Hardcover:
                    productTypes.Add(IntegrationDefaults.Hardcover, nameof(IntegrationDefaults.Hardcover));
                    break;
                case IntegrationDefaults.Audio_CD:
                    productTypes.Add(IntegrationDefaults.Audio_CD, nameof(IntegrationDefaults.Audio_CD).ChangeUnderscoreWithWhitespace());
                    break;
                case IntegrationDefaults.Audio_MP3:
                    productTypes.Add(IntegrationDefaults.Audio_MP3, nameof(IntegrationDefaults.Audio_MP3).ChangeUnderscoreWithWhitespace());
                    break;
            }

        return productTypes;
    }
}

//TODO: Return commented code when needed