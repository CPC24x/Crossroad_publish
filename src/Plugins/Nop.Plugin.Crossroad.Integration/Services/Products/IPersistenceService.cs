using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Crossroad.Integration.Services.Onix;
using static Nop.Plugin.Crossroad.Integration.Services.Onix.OnixEditProductsUpdateTask;

namespace Nop.Plugin.Crossroad.Integration.Services.Products;

public interface IPersistenceService
{
    Task PersistProducts(List<Contracts.CatalogueProductsResponse> catalogues,Action<ProgressReport> messages);

    Task UpdatePricesForBooksBasedOnTypes(Action<ProgressReport> reportProgress);
}