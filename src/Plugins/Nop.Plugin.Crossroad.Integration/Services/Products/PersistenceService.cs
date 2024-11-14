using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Crossroad.Integration.Infrastructure.Extensions;
using Nop.Plugin.Crossroad.Integration.Services.Manufacturer;
using Nop.Plugin.Crossroad.Integration.Services.Picture;
using Nop.Plugin.Crossroad.Integration.Services.SpecificationAttributes;
using Nop.Services.Catalog;
using Nop.Services.Media;
using static Nop.Plugin.Crossroad.Integration.Services.Onix.Contracts;

namespace Nop.Plugin.Crossroad.Integration.Services.Products;

public class PersistenceService : IPersistenceService
{
    private readonly IProductService _productService;
    private readonly IPictureService _pictureService;
    private readonly IPictureExtendedService _pictureExtendedService;
    private readonly IProductExtendedService _productAttributeServiceExtended;
    private readonly IWorkContext _workContext;
    private readonly CatalogSettings _catalogSettings;
    private readonly IStoreContext _storeContext;
    private readonly IManufacturerService _manufacturerService;
    private readonly IProductAttributeParser _productAttributeParser;
    private readonly IManufacturerExtendedService _manufacturerExtendedService;
    private readonly IProductSpecificationAttributeService _specificationAttributeOptionsService;
    private readonly ISpecificationAttributeService _specificationAttributeService;

    public PersistenceService(IProductService productService,
        IPictureService pictureService,
        IPictureExtendedService pictureExtendedService,
        IProductExtendedService productAttributeServiceExtended,
        IWorkContext workContext,
        CatalogSettings catalogSettings,
        IStoreContext storeContext,
        IManufacturerService manufacturerService,
        IProductAttributeParser productAttributeParser,
        IManufacturerExtendedService manufacturerExtendedService,
        IProductSpecificationAttributeService specificationAttributeOptionsService,
        ISpecificationAttributeService specificationAttributeService)
    {
        _productService = productService;
        _pictureService = pictureService;
        _pictureExtendedService = pictureExtendedService;
        _productAttributeServiceExtended = productAttributeServiceExtended;
        _workContext = workContext;
        _catalogSettings = catalogSettings;
        _storeContext = storeContext;
        _manufacturerService = manufacturerService;
        _productAttributeParser = productAttributeParser;
        _manufacturerExtendedService = manufacturerExtendedService;
        _specificationAttributeOptionsService = specificationAttributeOptionsService;
        _specificationAttributeService = specificationAttributeService;
    }

    public async Task PersistProducts(List<CatalogueProductsResponse> catalogues)
    {
        foreach (var catalogue in catalogues)
        {
            if (catalogue.SortFields.PublishingStatus == IntegrationDefaults.BOOK_PUBLISH_STATUS_KEY)
            {
                var bookDescriptions = catalogue.CollateralDetail
                                                                     .TextContent
                                                                     .Where(tt => tt.TextType.Type != IntegrationDefaults.BOOK_REVIEWS_KEY &&
                                                                                            tt.TextType.Type != IntegrationDefaults.BOOK_ENDORSMENT_KEY)
                                                                     .ToDictionary(tt => tt.TextType.Type,
                                                                                   tv => tv.Text.Description);

                int productId = await InsertOrUpdateProductAsync(catalogue, bookDescriptions[IntegrationDefaults.BOOK_DESCRIPTION_KEY]);

                if (catalogue.SortFields.CoverImage is not null &&
                    !string.IsNullOrWhiteSpace(catalogue.SortFields.CoverImage))
                {
                    int pictureId = await InsertOrUpdatePictureAsync(catalogue);

                    await InsertProductPictureAsync(pictureId, productId);
                }

                await InsertProductReviewAsync(productId, catalogue);

                await InsertBookAuthorsAsync(catalogue,
                                            _catalogSettings.DefaultManufacturerPageSize,
                                            _catalogSettings.DefaultManufacturerPageSizeOptions,
                                            productId);

                // publish date
                await AddSpecificationAttributeAsync("Publication date", catalogue.SortFields.PublicationDate.GetValueOrDefault().ToShortDateString(), productId, showOnProductPage: true);

                // title
                await AddSpecificationAttributeAsync("Title", catalogue.SortFields.Title, productId, showOnProductPage: true);

                // language
                await AddSpecificationAttributeFromListAsync(catalogue.DescriptiveDetail.Language, productId);

                // number of pages
                await AddSpecificationAttributeFromListAsync(catalogue.DescriptiveDetail.Extent, productId);

                // imprint
                await AddSpecificationAttributeFromListAsync(catalogue.PublishingDetail.Imprint, productId);

                string tableOfContent = string.Empty;

                if (bookDescriptions.TryGetValue(IntegrationDefaults.BOOK_TABLE_OF_CONTENT_KEY, out var tob))
                {
                    tableOfContent = tob;
                }

                // table of content
                await AddSpecificationAttributeAsync("Table of content", tableOfContent, productId);

                // endorsement
                await AddSpecificationAttributeFromListAsync(catalogue.CollateralDetail
                                                                                      .TextContent
                                                                                      .Where(tc => tc.TextType.Type == IntegrationDefaults.BOOK_ENDORSMENT_KEY)
                                                                                      .ToList(), productId);
                // publish status
                await InsertOrUpdatePublishingStatusAsync(catalogue, productId);

                // keywords
                await InsertOrUpdateProductKeywordsAsync(catalogue, "Keywords", productId);

                // product types
                await InsertProductTypesAsync(catalogue, productId);
            }
        }
    }

    public async Task UpdatePricesForBooksBasedOnTypes()
    {
        var productPrices = await _productAttributeServiceExtended.GetProductIds();

        foreach (var prodPrice in productPrices.ToList())
        {
            foreach (var prdPrice in prodPrice.Prices)
            {
                switch (prdPrice.Key)
                {
                    case "Epub":
                        prodPrice.Prices.Remove("Epub");
                        break;
                    case "AudioCD":
                        prodPrice.Prices.Remove("AudioCD");
                        break;
                    case "AudioMP3":
                        prodPrice.Prices.Remove("AudioMP3");
                        break;
                }
            }

            if (!prodPrice.Prices.Any())
            {
                var prodName = prodPrice.Name;

                var prodToDelete = productPrices.FirstOrDefault(x => x.Name == prodName);

                productPrices.Remove(prodToDelete);
            }

            // mora da se ispita da da li postoji bilo koji prices, a ako ne postoji da se obrise
        }

        foreach (var productPrice in productPrices)
        {
            var productId = await _productAttributeServiceExtended.GetProductIdByName(productPrice.Name);

            bool isProductAttributeExists = await _productAttributeServiceExtended.IsProductAttributeExists(IntegrationDefaults.ProductAttributeColumnName);

            ProductAttribute productAttribute = new()
            {
                Name = IntegrationDefaults.ProductAttributeColumnName
            };

            int productAttributeId;

            if (!isProductAttributeExists)
            {
                await _productAttributeServiceExtended.InsertProductAttributeAsync(productAttribute);

                productAttributeId = productAttribute.Id;
            }
            else
            {
                productAttributeId = await _productAttributeServiceExtended.GetProductAttributeIdByName(IntegrationDefaults.ProductAttributeColumnName);
            }

            bool isProductAttributeMappingExists = await _productAttributeServiceExtended.IsProductAttributeMappingExists(productAttributeId, productId);

            ProductAttributeMapping productAttributeMapping = new()
            {
                ProductAttributeId = productAttributeId,
                ProductId = productId,
                IsRequired = false,
                AttributeControlType = AttributeControlType.DropdownList,
                DisplayOrder = 0
            };

            int productAttributeMappingId;

            if (!isProductAttributeMappingExists)
            {
                await _productAttributeServiceExtended.InsertProductAttributeMappingAsync(productAttributeMapping);

                productAttributeMappingId = productAttributeMapping.Id;

                var prod = await _productService.GetProductByIdAsync(productId);

                await _productService.UpdateProductAsync(prod);
            }
            else
            {
                productAttributeMappingId = await _productAttributeServiceExtended.GetProductAttributeMappingByProductIdAndProductAttributeId(productAttributeId, productId);
            }

            foreach (var (priceKey, priceValue) in productPrice.Prices)
            {
                if (priceKey is "Paperback" or "Hardcover")
                {
                    // keys
                    var productAttributeValueFromDb = await _productAttributeServiceExtended.GetProductAttributeValueByNameAndProductAttributeMappingId(priceKey, productAttributeMappingId);

                    ProductAttributeValue productAttributeValue = new()
                    {
                        Name = priceKey,
                        ProductAttributeMappingId = productAttributeMappingId
                    };

                    if (productAttributeValueFromDb == null)
                    {
                        await _productAttributeServiceExtended.InsertProductAttributeValueAsync(productAttributeValue);
                    }
                    else
                    {
                        productAttributeValueFromDb.UpdateProductAttributeValueFromOnix(priceKey);

                        await _productAttributeServiceExtended.UpdateProductAttributeValueAsync(productAttributeValueFromDb);
                    }

                    // combination

                    var productAttributeMappings = await _productAttributeServiceExtended.GetProductAttributeMappingsByProductIdAsync(productId);

                    var existingCombinations = await _productAttributeServiceExtended.GetAllProductAttributeCombinationsAsync(productId);

                    foreach (var attributeMapping in productAttributeMappings)
                    {
                        var productAttributeValues = await _productAttributeServiceExtended.GetProductAttributeValuesAsync(attributeMapping.Id);

                        foreach (var attributeValue in productAttributeValues)
                        {
                            bool combinationExists = false;

                            foreach (var existingCombination in existingCombinations)
                            {
                                var parsedValues = (await _productAttributeParser.ParseProductAttributeValuesAsync(existingCombination.AttributesXml)).Select(pa => pa.Id);

                                if (parsedValues.Contains(attributeValue.Id))
                                {
                                    combinationExists = true;

                                    var attributeValueById = await _productAttributeServiceExtended.GetProductAttributeValueByIdAsync(attributeValue.Id);

                                    var prodId = (await _productAttributeServiceExtended.GetProductAttributeMappingByIdAsync(attributeValueById.ProductAttributeMappingId)).ProductId;

                                    var productName = (await _productService.GetProductByIdAsync(prodId)).Name;

                                    var typePricesByKey = productPrices.FirstOrDefault(pp => pp.Name == productName);

                                    var typePrice = typePricesByKey!.Prices[attributeValueById.Name];

                                    var productAttributeCombinationFromDb = await _productAttributeServiceExtended.GetProductAttributeCombinationByXmlAndProductId(existingCombination.AttributesXml, productId);

                                    productAttributeCombinationFromDb.UpdateProductAttributeCombination(typePrice.Prices);

                                    await _productAttributeServiceExtended.UpdateProductAttributeCombinationAsync(productAttributeCombinationFromDb);

                                    break;
                                }
                            }

                            if (!combinationExists)
                            {
                                var attributesXml = string.Empty;

                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, attributeMapping, attributeValue.Id.ToString());

                                var productAttributeCombinationFromDb = await _productAttributeServiceExtended.GetProductAttributeCombinationByXmlAndProductId(attributesXml, productId);

                                ProductAttributeCombination productAttributeCombination = new()
                                {
                                    ProductId = productId,
                                    PictureId = 0,
                                    OverriddenPrice = priceValue.Prices,
                                    StockQuantity = 100,
                                    MinStockQuantity = 1,
                                    Sku = priceValue.Sku,
                                    AttributesXml = attributesXml,
                                };

                                if (productAttributeCombinationFromDb == null)
                                {
                                    await _productAttributeServiceExtended.InsertProductAttributeCombinationAsync(productAttributeCombination);
                                }
                            }
                        }
                    }
                }
            }

            // formats
            await AddSpecificationAttributeAsync("Formats",
                await _productAttributeServiceExtended.GetProductIsbnAndTypeAsync(productId),
                productId, showOnProductPage: true);

            var product = await _productAttributeServiceExtended.GetTrimSizesByProductName(productId);

            // trim size
            await AddSpecificationAttributeAsync("Trim size", $"{product.height} x {product.width}", productId, showOnProductPage: true);

            var prd = await _productService.GetProductByIdAsync(productId);

            prd.Published = true;
            prd.ShowOnHomepage = true;
            prd.VisibleIndividually = true;

            await _productService.UpdateProductAsync(prd);
        }
    }

    private async Task InsertOrUpdateProductKeywordsAsync(CatalogueProductsResponse onixProduct,
                                                          string productAttributeColumnName,
                                                          int productId)
    {
        // specification attribute
        bool isSpecificationAttributeExists = await _specificationAttributeOptionsService.IsSpecificationAttributeExists(productAttributeColumnName);

        SpecificationAttribute specificationAttribute = new()
        {
            Name = productAttributeColumnName,
            DisplayOrder = 1
        };

        int specificationAttributeId;

        if (!isSpecificationAttributeExists)
        {
            await _specificationAttributeService.InsertSpecificationAttributeAsync(specificationAttribute);

            specificationAttributeId = specificationAttribute.Id;
        }
        else
        {
            specificationAttributeId = (await _specificationAttributeService.GetSpecificationAttributesAsync()).FirstOrDefault(sa => sa.Name == productAttributeColumnName)!.Id;
        }

        // specification attribute option

        Dictionary<string, string> keywordsDictionary = GetBookSubjects(onixProduct);

        if (keywordsDictionary.TryGetValue("Keywords", out var keywords))
        {
            string[] splitKeywords = keywords.StringToArray();

            foreach (var splitKeyword in splitKeywords)
            {
                bool isSpecificationAttributesOptionsExists = await _specificationAttributeOptionsService.IsSpecificationAttributeOptionsExists(splitKeyword, specificationAttributeId);

                SpecificationAttributeOption specificationAttributeOption = new()
                {
                    Name = splitKeyword,
                    SpecificationAttributeId = specificationAttributeId,
                    DisplayOrder = 1
                };

                int specificationAttributeOptionId;

                if (!isSpecificationAttributesOptionsExists)
                {
                    await _specificationAttributeService.InsertSpecificationAttributeOptionAsync(specificationAttributeOption);

                    specificationAttributeOptionId = specificationAttributeOption.Id;
                }
                else
                {
                    specificationAttributeOptionId = await _specificationAttributeOptionsService.GetSpecificationAttributeOptionIdByName(splitKeyword);
                }

                // specification attribute mapping

                bool isSpecificationAttributeMappingExists = await _specificationAttributeOptionsService.IsSpecificationAttributeMappingExists(productId, specificationAttributeOptionId);

                if (!isSpecificationAttributeMappingExists)
                {
                    await _specificationAttributeService.InsertProductSpecificationAttributeAsync(new ProductSpecificationAttribute
                    {
                        ProductId = productId,
                        AttributeTypeId = 0,
                        SpecificationAttributeOptionId = specificationAttributeOptionId,
                        CustomValue = null,
                        AllowFiltering = true,
                        ShowOnProductPage = false,
                        DisplayOrder = 1
                    });
                }
            }
        }
    }

    private async Task<int> InsertOrUpdateProductAsync(CatalogueProductsResponse onixProduct, string bookDescription)
    {
        var measures = onixProduct.DescriptiveDetail.Measure;

        decimal width = default;

        decimal height = default;

        if (measures is not null)
        {
            var bookSize = measures.ToDictionary(key => key.Type.Value, value => value.Measurement.Measure!);

            if (bookSize.ContainsKey(IntegrationDefaults.BOOK_WIDTH_KEY))
            {
                if (bookSize.TryGetValue(IntegrationDefaults.BOOK_WIDTH_KEY, out var widthByKey))
                {
                    width = decimal.TryParse(widthByKey, out var widthValue) ? widthValue : 0;
                }
            }

            if (bookSize.ContainsKey(IntegrationDefaults.BOOK_HEIGHT_KEY))
            {
                if (bookSize.TryGetValue(IntegrationDefaults.BOOK_HEIGHT_KEY, out var heightByKey))
                {
                    height = decimal.TryParse(heightByKey, out var heightValue) ? heightValue : 0;
                }
            }
        }

        Product product = new()
        {
            Sku = onixProduct.SortFields.ISBN13,
            Name = onixProduct.SortFields.Title,
            ManufacturerPartNumber = onixProduct.Id,
            Price = decimal.Parse(onixProduct.ProductSupply.FirstOrDefault()!
                .SupplyDetail.FirstOrDefault()!
                .Price.FirstOrDefault()!
                .PriceAmount.Price!),
            Width = width,
            Height = height,
            ShortDescription = bookDescription,
            FullDescription = bookDescription,
            DisableBuyButton = onixProduct.SortFields.PublishingStatus != IntegrationDefaults.BOOK_PUBLISH_STATUS_KEY,
            ManageInventoryMethod = ManageInventoryMethod.ManageStock,
            StockQuantity = int.MaxValue,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = int.MaxValue
        };

        var productFromDb = await _productService.GetProductBySkuAsync(product.Sku);

        if (productFromDb == null)
        {
            await _productService.InsertProductAsync(product);

            return product.Id;
        }

        productFromDb.UpdateProductFromOnix(onixProduct, width, height, bookDescription);

        await _productService.UpdateProductAsync(productFromDb);

        return productFromDb.Id;
    }

    private async Task<int> InsertOrUpdatePictureAsync(CatalogueProductsResponse onixProduct)
    {
        var imageBytes = await onixProduct.SortFields.CoverImage.GetBytesFromUrlAsync();

        var seoName = onixProduct.SortFields.Title.ToLower().Replace(" ", "_");

        Core.Domain.Media.Picture pictureFromDb = await _pictureExtendedService.GetPictureBySeoName(seoName);

        Core.Domain.Media.Picture bookPicture;

        if (pictureFromDb == null)
        {
            bookPicture = await _pictureService.InsertPictureAsync(imageBytes, MimeTypes.ImageJpeg, seoName);

            return bookPicture.Id;
        }

        pictureFromDb.UpdatePictureFromOnix(seoName, MimeTypes.ImageJpeg);

        await _pictureService.UpdatePictureAsync(pictureFromDb);

        return pictureFromDb.Id;
    }

    private async Task InsertProductPictureAsync(int bookPictureId, int productId)
    {
        bool isProductPictureExists = await _pictureExtendedService.IsProductPictureExists(bookPictureId, productId);

        if (!isProductPictureExists)
        {
            await _productService.InsertProductPictureAsync(new ProductPicture
            {
                DisplayOrder = 0,
                PictureId = bookPictureId,
                ProductId = productId
            });
        }
    }

    private async Task InsertProductReviewAsync(int productId, CatalogueProductsResponse response)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();

        var store = await _storeContext.GetCurrentStoreAsync();

        var reviews = response.CollateralDetail
                                                 .TextContent
                                                 .Where(tt => tt.TextType.Type == IntegrationDefaults.BOOK_REVIEWS_KEY)
                                                 .ToLookup(tt => tt.TextType.Type,
                                                           tv => tv.Text.Description);


        bool isProductReviewExist = (await _productService.GetAllProductReviewsAsync()).Any(pr => pr.ProductId == productId);

        foreach (var review in reviews)
        {
            foreach (var reviewValue in review)
            {
                if (!isProductReviewExist && !string.IsNullOrWhiteSpace(reviewValue))
                {
                    await _productService.InsertProductReviewAsync(new ProductReview
                    {
                        ProductId = productId,
                        CustomerId = customer.Id,
                        Title = nameof(Product.ShortDescription),
                        ReviewText = reviewValue,
                        Rating = _catalogSettings.DefaultProductRatingValue,
                        HelpfulYesTotal = 0,
                        HelpfulNoTotal = 0,
                        IsApproved = true,
                        CreatedOnUtc = DateTime.UtcNow,
                        StoreId = store.Id
                    });
                }
            }
        }
    }

    private async Task InsertBookAuthorsAsync(CatalogueProductsResponse onixProduct,
                                              int pageSize,
                                              string pageOptions,
                                              int productId)
    {
        var authors = onixProduct.DescriptiveDetail
            .Contributor
            .Select(author => new
            {
                AuthorName = author.AuthorName.Value,
                AuthorNameInverted = author.AuthorNameInverted.Value,
                AuthoRole = author.ContributorRole.Value
            });

        foreach (var author in authors)
        {
            var manufacturerFromDb = await _manufacturerExtendedService.GetManufacturerByNameAsync(author.AuthorName);

            Core.Domain.Catalog.Manufacturer manufacturer = new()
            {
                Name = author.AuthorName,
                CreatedOnUtc = DateTime.UtcNow,
                PageSize = pageSize,
                PageSizeOptions = pageOptions,
                Published = true,
                AllowCustomersToSelectPageSize = true
            };

            int manufacturerId;

            if (manufacturerFromDb is null)
            {
                await _manufacturerService.InsertManufacturerAsync(manufacturer);

                manufacturerId = manufacturer.Id;

                await AddSpecificationAttributeAsync(nameof(author.AuthorNameInverted), author.AuthorNameInverted, productId);

                await AddSpecificationAttributeAsync(nameof(author.AuthoRole), author.AuthoRole, productId);
            }
            else
            {
                manufacturerId = (await _manufacturerExtendedService.GetManufacturerByNameAsync(author.AuthorName)).Id;
            }

            await _manufacturerService.InsertProductManufacturerAsync(new ProductManufacturer
            {
                IsFeaturedProduct = true,
                DisplayOrder = 1,
                ProductId = productId,
                ManufacturerId = manufacturerId
            });
        }
    }

    private async Task InsertProductTypesAsync(CatalogueProductsResponse onixProduct, int productId)
    {
        var productFormTypes = onixProduct.DescriptiveDetail.ProductFormDetail.ProductFromTypesToDictionary();

        foreach (var productFormType in productFormTypes.Values)
        {
            var columnName = IntegrationDefaults.ProductAttributeColumnName;

            bool isSpecificationAttributeExists = await _specificationAttributeOptionsService.IsSpecificationAttributeExists(columnName);

            SpecificationAttribute specificationAttribute = new()
            {
                Name = columnName,
                DisplayOrder = 1
            };

            int specificationAttributeId;

            if (!isSpecificationAttributeExists)
            {
                await _specificationAttributeService.InsertSpecificationAttributeAsync(specificationAttribute);

                specificationAttributeId = specificationAttribute.Id;
            }
            else
            {
                specificationAttributeId = (await _specificationAttributeService.GetSpecificationAttributesAsync()).FirstOrDefault(sa => sa.Name == columnName)!.Id;
            }

            // specification attribute option

            bool isSpecificationAttributesOptionsExists = await _specificationAttributeOptionsService.IsSpecificationAttributeOptionsExists(productFormType, specificationAttributeId);

            SpecificationAttributeOption specificationAttributeOption = new()
            {
                Name = productFormType,
                SpecificationAttributeId = specificationAttributeId,
                DisplayOrder = 1
            };

            int specificationAttributeOptionId;

            if (!isSpecificationAttributesOptionsExists)
            {
                await _specificationAttributeService.InsertSpecificationAttributeOptionAsync(specificationAttributeOption);

                specificationAttributeOptionId = specificationAttributeOption.Id;
            }
            else
            {
                specificationAttributeOptionId = (await _specificationAttributeOptionsService.GetSpecificationAttributeOptionIdByName(productFormType));
            }

            // specification attribute mapping

            bool isSpecificationAttributeMappingExists = await _specificationAttributeOptionsService.IsSpecificationAttributeMappingExists(productId, specificationAttributeOptionId);

            if (!isSpecificationAttributeMappingExists)
            {
                await _specificationAttributeService.InsertProductSpecificationAttributeAsync(new ProductSpecificationAttribute
                {
                    ProductId = productId,
                    AttributeTypeId = 0,
                    SpecificationAttributeOptionId = specificationAttributeOptionId,
                    CustomValue = null,
                    AllowFiltering = false,
                    ShowOnProductPage = false,
                    DisplayOrder = 1
                });
            }
        }
    }

    private async Task AddSpecificationAttributeFromListAsync<T>(List<T> onixResponseValues, int productId)
    {
        switch (onixResponseValues)
        {
            case List<Language> languages:
                foreach (var language in languages)
                    await AddSpecificationAttributeAsync(nameof(language.LanguageCode.Language), language.LanguageCode.Language, productId, showOnProductPage: true);
                break;

            case List<Extent> numbersOfPage:
                foreach (var extent in numbersOfPage)
                    await AddSpecificationAttributeAsync("Number of pages", extent.NumberOfPages.Number, productId, showOnProductPage: true);
                break;

            case List<Imprint> imprints:
                foreach (var imprint in imprints)
                    await AddSpecificationAttributeAsync("Imprint", imprint.ImprintName.Name, productId, showOnProductPage: true);
                break;

            case List<TextContent> texts:
                foreach (var text in texts)
                    await AddSpecificationAttributeAsync("Endorsement", text.Text.Description, productId);
                break;
        }
    }

    private async Task InsertOrUpdatePublishingStatusAsync(CatalogueProductsResponse onixProduct, int productId)
    {
        var columName = nameof(onixProduct.SortFields.PublishingStatus);

        var specificationAttribute = await _specificationAttributeOptionsService.GetSpecificationAttributeByName(columName);

        string publishStatusValue = (onixProduct.SortFields.PublishingStatus == IntegrationDefaults.BOOK_PUBLISH_STATUS_KEY).ToString();

        if (specificationAttribute == null)
        {
            await AddSpecificationAttributeAsync(columName, publishStatusValue, productId);

            return;
        }

        var specificationAttributeOptions = await _specificationAttributeOptionsService.GetSpecificationAttributeOptionsBySpecificationAttributeId(specificationAttribute.Id);

        if (specificationAttributeOptions == null)
        {
            await _specificationAttributeService.InsertSpecificationAttributeOptionAsync(
                new SpecificationAttributeOption
                {
                    DisplayOrder = 1,
                    Name = publishStatusValue,
                    SpecificationAttributeId = specificationAttribute.Id
                });

            return;
        }

        specificationAttributeOptions.Name = publishStatusValue;

        await _specificationAttributeService.UpdateSpecificationAttributeOptionAsync(specificationAttributeOptions);
    }

    private Dictionary<string, string> GetBookSubjects(CatalogueProductsResponse response)
    {
        var onixKeywords = response.DescriptiveDetail.Subject;

        Dictionary<string, string> keywords = new();

        foreach (var onixKeyword in onixKeywords)
        {
            if (onixKeyword is { Keywords: not null, SchemeName: not null })
            {
                keywords.Add(onixKeyword.SchemeName.Name, onixKeyword.Keywords.KeywordsValues);
            }
        }

        return keywords;
    }

    private async Task AddSpecificationAttributeAsync(string columnName, string columnValue, int productId, string alternativeColumName = "", bool showOnProductPage = false)
    {
        // specification attribute
        columnName = string.IsNullOrWhiteSpace(alternativeColumName) ? columnName : alternativeColumName;

        bool isSpecificationAttributeExists = await _specificationAttributeOptionsService.IsSpecificationAttributeExists(columnName);

        SpecificationAttribute specificationAttribute = new()
        {
            Name = columnName,
            DisplayOrder = 1
        };

        int specificationAttributeId;

        if (!isSpecificationAttributeExists)
        {
            await _specificationAttributeService.InsertSpecificationAttributeAsync(specificationAttribute);

            specificationAttributeId = specificationAttribute.Id;
        }
        else
        {
            specificationAttributeId = (await _specificationAttributeService.GetSpecificationAttributesAsync()).FirstOrDefault(sa => sa.Name == columnName)!.Id;
        }

        // specification attribute option

        bool isSpecificationAttributesOptionsExists = await _specificationAttributeOptionsService.IsSpecificationAttributeOptionsExists(columnValue, specificationAttributeId);

        SpecificationAttributeOption specificationAttributeOption = new()
        {
            Name = columnValue,
            SpecificationAttributeId = specificationAttributeId,
            DisplayOrder = 1
        };

        int specificationAttributeOptionId;

        if (!isSpecificationAttributesOptionsExists)
        {
            await _specificationAttributeService.InsertSpecificationAttributeOptionAsync(specificationAttributeOption);

            specificationAttributeOptionId = specificationAttributeOption.Id;
        }
        else
        {
            specificationAttributeOptionId = (await _specificationAttributeOptionsService.GetSpecificationAttributeOptionIdByName(columnValue));
        }

        // specification attribute mapping

        bool isSpecificationAttributeMappingExists = await _specificationAttributeOptionsService.IsSpecificationAttributeMappingExists(productId, specificationAttributeOptionId);

        if (!isSpecificationAttributeMappingExists)
        {
            await _specificationAttributeService.InsertProductSpecificationAttributeAsync(new ProductSpecificationAttribute
            {
                ProductId = productId,
                AttributeTypeId = 0,
                SpecificationAttributeOptionId = specificationAttributeOptionId,
                CustomValue = null,
                AllowFiltering = false,
                ShowOnProductPage = showOnProductPage,
                DisplayOrder = 1
            });
        }
    }
}