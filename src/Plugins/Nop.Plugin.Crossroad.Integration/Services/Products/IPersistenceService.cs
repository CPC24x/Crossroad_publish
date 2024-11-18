using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Crossroad.Integration.Services.Onix;

namespace Nop.Plugin.Crossroad.Integration.Services.Products;

public interface IPersistenceService
{
    Task PersistProducts(List<Contracts.CatalogueProductsResponse> catalogues,Action<List<string>> messages);

    Task UpdatePricesForBooksBasedOnTypes();
}