using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using static Nop.Plugin.Crossroad.Integration.Services.Onix.Contracts;

namespace Nop.Plugin.Crossroad.Integration.Infrastructure.Extensions;

public static class OnixExtension
{
    public static void UpdateProductFromOnix(this Product product,
        CatalogueProductsResponse onixResponse,
        decimal width,
        decimal height,
        string bookDescription)
    {
        product.Sku = onixResponse.Id;
        product.Name = onixResponse.SortFields.Title;
        product.ManufacturerPartNumber = onixResponse.SortFields.ISBN13;
        product.Price = decimal.Parse(onixResponse.ProductSupply.FirstOrDefault()!
            .SupplyDetail.FirstOrDefault()!
            .Price.FirstOrDefault()!
            .PriceAmount.Price!);
        product.Width = width;
        product.Height = height;
        product.ShortDescription = bookDescription;
    }

    public static void UpdatePictureFromOnix(this Picture picture,
                                                  string seoName,
                                                  string mimeType)
    {
        picture.SeoFilename = seoName;
        picture.MimeType = mimeType;
    }

    public static void UpdateProductAttributeValueFromOnix(this ProductAttributeValue productAttributeValue, string name) => productAttributeValue.Name = name;

    public static void UpdateProductAttributeCombination(this ProductAttributeCombination productAttributeCombination, decimal price) => productAttributeCombination.OverriddenPrice = price;
}