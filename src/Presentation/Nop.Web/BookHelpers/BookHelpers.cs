// panorazzi

using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Nop.Web.BookHelpers
{
    public class BookHelpers
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;

        public BookHelpers(ISpecificationAttributeService specificationAttributeService)
        {
            _specificationAttributeService = specificationAttributeService;
        }

        public async Task<string> GetTitleAsync(int bookId)
        {
            // use sepecification to SpecificationAttribute.Name = Title
            var sa = await GetSpecificationAttributeOptionByAttributeNameAsync("Title");

            if (sa != null)
                return $"Title: {sa.Name}";

            return "";
        }

        private async Task<SpecificationAttributeOption> GetSpecificationAttributeOptionByAttributeNameAsync(string attributeName)
        {
            return null;
        }
    }
}