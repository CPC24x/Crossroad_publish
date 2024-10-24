using System.Threading.Tasks;
using Nop.Services.Catalog;

namespace Nop.Plugin.Crossroad.Integration.Services.Manufacturer;

public interface IManufacturerExtendedService : IManufacturerService
{
    Task<Core.Domain.Catalog.Manufacturer> GetManufacturerByNameAsync(string name);
}