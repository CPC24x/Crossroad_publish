using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using LinqToDB;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Crossroad.Integration.Helpers;
using Nop.Plugin.Crossroad.Integration.Infrastructure.Extensions;
using Nop.Plugin.Crossroad.Integration.Services.Manufacturer;
using Nop.Plugin.Crossroad.Integration.Services.Picture;
using Nop.Plugin.Crossroad.Integration.Services.SpecificationAttributes;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Seo;
using static Nop.Plugin.Crossroad.Integration.Services.Onix.Contracts;
using static Nop.Plugin.Crossroad.Integration.Services.Onix.OnixEditProductsUpdateTask;

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
    private readonly IRepository<Product> _productRepository;
    private readonly IUrlRecordService _urlRecordService;
    private readonly IProductTemplateService _productTemplateService;
    private readonly ImageUrlChecker _imageUrlChecker;

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
        ISpecificationAttributeService specificationAttributeService,
        IRepository<Product> productRepository,
        IUrlRecordService urlRecordService,
        IProductTemplateService productTemplateService,
        ImageUrlChecker imageUrlChecker)
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
        _productRepository = productRepository;
        _urlRecordService = urlRecordService;
        _productTemplateService = productTemplateService;
        _imageUrlChecker = imageUrlChecker;
    }

    public async Task PersistProducts(List<CatalogueProductsResponse> catalogues, Action<ProgressReport> reportProgress, bool checkTitlecode = true)
    {
        foreach (var catalogue in catalogues)
        {
            reportProgress(new ProgressReport($"Start sync {catalogue.SortFields.Title}"));

            if (new OE_PublishingStatus_Enum().GetKeys("Active").Contains(catalogue.SortFields.PublishingStatus))
            {
                try
                {
                    var titleCode = catalogue.ProductIdentifier.FirstOrDefault(x => x.IDTypeName?.Value == "Title Code")?.IDValue?.Value;

                    if (titleCode == null && checkTitlecode)
                    {
                        reportProgress(new ProgressReport($"{catalogue.SortFields.Title} ISBN13:{catalogue.SortFields?.ISBN13 ?? ""} no Title Code", false));
                        continue;
                    }


                    var bookDescriptions = catalogue.CollateralDetail
                                            .TextContent
                                            .Where(tt => !new OE_TextType_Enum().GetKeys("Review quote").Contains(tt?.TextType?.Type) && !new OE_TextType_Enum().GetKeys("Endorsement").Contains(tt?.TextType?.Type))
                                            ?.ToDictionary(tt => tt?.TextType?.Type,
                                                          tv => tv?.Text?.Description);

                    int productId = await InsertOrUpdateProductAsync(catalogue, bookDescriptions);

                    // product types
                    var productName = await InsertProductTypesAsync(catalogue, productId);

                    _imageUrlChecker.ImageUrls.Clear();
                    if (catalogue.SortFields.CoverImage is not null && !string.IsNullOrWhiteSpace(catalogue.SortFields.CoverImage))
                    {
                        var url = _imageUrlChecker.AddUrlToDictionary(catalogue?.SortFields?.CoverImage);
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            int pictureId = await InsertOrUpdatePictureAsync(url: url, seoName: productName);
                            await InsertProductPictureAsync(pictureId, productId);
                        }

                    }

                    foreach (var supportingResource in catalogue.CollateralDetail.SupportingResource)
                    {
                        foreach (var resourceVersion in supportingResource.ResourceVersion)
                        {

                            var url = _imageUrlChecker.AddUrlToDictionary(resourceVersion?.ResourceLink?.Value ?? "");
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                int lastSlashIndex = url.LastIndexOf('/');
                                var filename = url.Substring(lastSlashIndex + 1);
                                filename = Path.GetFileNameWithoutExtension(filename);
                                filename = Regex.Replace(filename, "_[^_]*$", "");
                                int pictureId = await InsertOrUpdatePictureAsync(url: url, seoName: filename);
                                await InsertProductPictureAsync(pictureId, productId);
                            }
                        }
                    }

                    await InsertProductReviewAsync(productId, catalogue);

                    var authors = await InsertBookAuthorsAsync(catalogue,
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

                    foreach (var item in bookDescriptions.Where(x => new OE_TextType_Enum().GetKeys("Table of contents").Contains(x.Key)))
                    {
                        tableOfContent = item.Value;
                    }

                    // table of content
                    await AddSpecificationAttributeAsync("Table of content", tableOfContent, productId);

                    // endorsement
                    await AddSpecificationAttributeFromListAsync(catalogue.CollateralDetail
                                                                                          .TextContent
                                                                                          .Where(tc => new OE_TextType_Enum().GetKeys("Endorsement").Contains(tc.TextType.Type))
                                                                                          .ToList(), productId);

                    //26/11/2024 add
                    await AddSpecificationAttributeAsync("ISBN13", catalogue.SortFields?.ISBN13 ?? "", productId, showOnProductPage: true);
                    await AddSpecificationAttributeAsync("ISBN10", catalogue.SortFields?.ISBN10 ?? "", productId, showOnProductPage: true);
                    await AddSpecificationAttributeAsync("Edition Number", catalogue.DescriptiveDetail?.EditionNumber?.Value ?? "", productId, showOnProductPage: true);
                    await AddSpecificationAttributeAsync("Edition Statement", catalogue.DescriptiveDetail?.EditionStatement?.Value ?? "", productId, showOnProductPage: true);
                    await AddSpecificationAttributeFromListAsync(catalogue.ProductIdentifier, productId);
                    await AddSpecificationAttributeFromListAsync(catalogue.DescriptiveDetail.ProductFormDetail, productId);
                    await AddSpecificationAttributeFromListAsync(catalogue.DescriptiveDetail.TitleDetail, productId);
                    await AddSpecificationAttributeFromListAsync(catalogue.DescriptiveDetail.Contributor, productId);
                    await AddSpecificationAttributeFromListAsync(catalogue.DescriptiveDetail.AncillaryContent, productId);
                    await AddSpecificationAttributeFromListAsync(catalogue.PublishingDetail.PublishingDate, productId);
                    await AddSpecificationAttributeFromListAsync(catalogue.ProductSupply, productId);
                    //await AddSpecificationAttributeFromListAsync(catalogue.DescriptiveDetail.Subject, productId);
                    await AddSpecificationAttributeFromListAsync(catalogue.CollateralDetail.Prize, productId);
                    //26/11/2024 add

                    // publish status
                    await InsertOrUpdatePublishingStatusAsync(catalogue, productId);

                    // keywords
                    await InsertOrUpdateProductKeywordsOrBISACAsync(catalogue, productId);

                    reportProgress(new ProgressReport($"Start sync {catalogue.SortFields.Title} ISBN13:{catalogue.SortFields?.ISBN13 ?? ""} sync complete"));
                }
                catch (Exception ex)
                {
                    reportProgress(new ProgressReport($"{catalogue.SortFields.Title} ISBN13:{catalogue.SortFields?.ISBN13 ?? ""} sync exception {ex.Message}", false));
                }

            }
            else
                reportProgress(new ProgressReport($"{catalogue.SortFields.Title} ISBN13:{catalogue.SortFields?.ISBN13 ?? ""} no publish", false));

        }
    }

    public async Task UpdatePricesForBooksBasedOnTypes(Action<ProgressReport> reportProgress)
    {
        var productPrices = await _productAttributeServiceExtended.GetProductIds();

        foreach (var prodPrice in productPrices.ToList())
        {
            foreach (var prdPrice in prodPrice.Prices)
            {
                switch (prdPrice.Key)
                {
                    case "Epub": //{"E101", "EPUB"},
                        prodPrice.Prices.Remove("Epub");
                        break;
                    case "AudioCD": //CD standard audio format
                        prodPrice.Prices.Remove("AudioCD");
                        break;
                    case "AudioMP3": //MP3 format
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
            reportProgress(new ProgressReport($"Updating price for {productPrice.Name}"));

            try
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

                reportProgress(new ProgressReport($"Price update for {productPrice.Name} complete"));
            }
            catch (Exception ex)
            {
                reportProgress(new ProgressReport($"Price update for {productPrice.Name} failed with exception. {ex.Message}", false));
            }

        }
    }

    private async Task InsertOrUpdateProductKeywordsOrBISACAsync(CatalogueProductsResponse onixProduct, int productId)
    {
        // specification attribute


        // specification attribute option

        Dictionary<string, string> subjectDictionary = GetBookSubjects(onixProduct);

        if (subjectDictionary is not null)
        {
            foreach (var subject in subjectDictionary)
            {

                bool isSpecificationAttributeExists = await _specificationAttributeOptionsService.IsSpecificationAttributeExists(subject.Key);

                SpecificationAttribute specificationAttribute = new()
                {
                    Name = subject.Key,
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
                    specificationAttributeId = (await _specificationAttributeService.GetSpecificationAttributesAsync()).FirstOrDefault(sa => sa.Name == subject.Key)!.Id;
                }


                string[] splitKeywords = subject.Value.StringToArray();

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
    }

    private async Task<int> InsertOrUpdateProductAsync(CatalogueProductsResponse onixProduct, Dictionary<string, string> bookDescriptions)
    {
        var measures = onixProduct.DescriptiveDetail.Measure;
        var bookFullDescription = bookDescriptions.FirstOrDefault(x => new OE_TextType_Enum().GetKeys("Description").Contains(x.Key)).Value;
        //var bookShortDescription = bookDescriptions.FirstOrDefault(x => new OE_TextType_Enum().GetKeys("Description").Contains(x.Key)).Value;
        decimal width = 0;

        decimal height = 0;

        if (measures is not null)
        {
            var bookSize = measures.ToDictionary(key => key.Type.Value, value => value.Measurement.Measure!);

            foreach (var item in bookSize.Where(x => new OE_MeasureType_Enum().GetKeys("Weight").Contains(x.Key)))
            {
                width = decimal.TryParse(item.Value, out var widthValue) ? widthValue : 0;
            }

            foreach (var item in bookSize.Where(x => new OE_MeasureType_Enum().GetKeys("Height").Contains(x.Key)))
            {
                height = decimal.TryParse(item.Value, out var heightValue) ? heightValue : 0;
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
            ShortDescription = bookFullDescription,
            FullDescription = bookFullDescription,
            DisableBuyButton = !new OE_PublishingStatus_Enum().GetKeys("Active").Contains(onixProduct.SortFields.PublishingStatus),
            ManageInventoryMethod = ManageInventoryMethod.ManageStock,
            StockQuantity = int.MaxValue,
            VisibleIndividually = false,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = int.MaxValue,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow,
            Published = true,
            ProductType = ProductType.SimpleProduct,
            ProductTypeId = (int)ProductType.SimpleProduct,
        };

        var productFromDb = await _productService.GetProductBySkuAsync(product.Sku);

        if (productFromDb == null)
        {
            await _productService.InsertProductAsync(product);
            var seName = await _urlRecordService.ValidateSeNameAsync(product, null, product.Name, true);
            await _urlRecordService.SaveSlugAsync(product, seName, 0);
            return product.Id;
        }

        productFromDb.UpdateProductFromOnix(onixProduct, width, height, bookFullDescription);

        productFromDb.UpdatedOnUtc = DateTime.UtcNow;
        await _productService.UpdateProductAsync(productFromDb);

        return productFromDb.Id;
    }

    private async Task<int> InsertOrUpdatePictureAsync(string url, string seoName)
    {
        var imageBytes = await url.GetBytesFromUrlAsync();

        string invalidChars = @"[<>:""/\\|?*]";
        seoName = Regex.Replace(seoName, invalidChars, "");
        seoName = seoName.ToLower().Replace(" ", "_");

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
                                                 .Where(tt => new OE_TextType_Enum().GetKeys("Review quote").Contains(tt.TextType.Type))
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

    private async Task<IList<string>> InsertBookAuthorsAsync(CatalogueProductsResponse onixProduct,
                                              int pageSize,
                                              string pageOptions,
                                              int productId)
    {
        var selectedManufacturerIds = await _manufacturerService.GetProductManufacturersByProductIdAsync(productId, true);

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

                await AddSpecificationAttributeAsync("Person Name Inverted", author.AuthorNameInverted, productId);

                await AddSpecificationAttributeAsync("Contributor Role", new OE_ContributorRole_Enum().GetValue(author.AuthoRole), productId);
            }
            else
            {
                manufacturerId = (await _manufacturerExtendedService.GetManufacturerByNameAsync(author.AuthorName)).Id;
            }

            if (!selectedManufacturerIds.Any(x => x.ManufacturerId == manufacturerId))
            {
                await _manufacturerService.InsertProductManufacturerAsync(new ProductManufacturer
                {
                    IsFeaturedProduct = true,
                    DisplayOrder = 1,
                    ProductId = productId,
                    ManufacturerId = manufacturerId
                });
            }

        }

        foreach (var selectedManufacturerId in selectedManufacturerIds)
        {
            var manufacturerFromDb = await _manufacturerExtendedService.GetManufacturerByIdAsync(selectedManufacturerId.ManufacturerId);
            if (!authors.Any(x => x.AuthorName == manufacturerFromDb.Name))
                await _manufacturerExtendedService.DeleteProductManufacturerAsync(selectedManufacturerId);
        }

        return authors.Select(x => x.AuthorName).ToList();
    }

    private async Task<string> InsertProductTypesAsync(CatalogueProductsResponse onixProduct, int productId)
    {
        var productFormTypes = new OE_ProductFormDetail_Enum().GetDictionary(onixProduct.DescriptiveDetail.ProductFormDetail.Select(x => x.ProductFormCode).ToList());

        var product = await _productService.GetProductByIdAsync(productId);
        product.Name += $" ({productFormTypes.Values.FirstOrDefault()})";

        await _productService.UpdateProductAsync(product);
        return product.Name;
    }

    private async Task AddSpecificationAttributeFromListAsync<T>(List<T> onixResponseValues, int productId)
    {
        switch (onixResponseValues)
        {
            case List<Language> languages:
                foreach (var language in languages)
                    await AddSpecificationAttributeAsync("Language", language.LanguageCode.Language, productId, showOnProductPage: true);
                break;

            case List<Extent> extents:
                foreach (var extent in extents)
                    await AddSpecificationAttributeAsync(new OE_ExtentType_Enum().GetValue(extent.ExtentType.Value), extent.ExtentValue.Value + new OE_ExtentUnit_Enum().GetValue(extent.ExtentUnit.Value), productId, showOnProductPage: true);
                break;

            case List<Imprint> imprints:
                foreach (var imprint in imprints)
                    await AddSpecificationAttributeAsync("Imprint name", imprint.ImprintName.Name, productId, showOnProductPage: true);
                break;

            case List<PublishingDate> publishingDates:
                foreach (var publishingDate in publishingDates)
                    await AddSpecificationAttributeAsync("Publishing Date", publishingDate.Date?.ISODate.GetValueOrDefault().ToShortDateString(), productId, showOnProductPage: true);
                break;

            case List<TextContent> texts:
                foreach (var text in texts)
                    await AddSpecificationAttributeAsync("Endorsement", text.Text.Description, productId);
                break;

            case List<ProductFormDetail> productFormDetails:
                foreach (var productFormDetail in productFormDetails)
                    await AddSpecificationAttributeAsync("Product Form", new OE_ProductFormDetail_Enum().GetValue(productFormDetail.ProductFormCode), productId, showOnProductPage: true);
                break;

            case List<TitleDetail> titleDetails:
                foreach (var titleDetail in titleDetails)
                {
                    foreach (var titleElement in titleDetail.TitleElement)
                    {
                        await AddSpecificationAttributeAsync("Without Prefix", titleElement?.TitleWithoutPrefix?.TitleWithoutPrefixValue, productId, showOnProductPage: true);
                        await AddSpecificationAttributeAsync("With Prefix", titleElement?.TitlePrefix?.TitlePrefixValue ?? "", productId, showOnProductPage: true);
                        await AddSpecificationAttributeAsync("Subtitle", titleElement?.Subtitle?.SubtitleValue ?? "", productId, showOnProductPage: true);
                    }
                }
                break;

            case List<Contributor> contributors:
                foreach (var contributor in contributors)
                {
                    await AddSpecificationAttributeAsync("Contributor Role", new OE_ContributorRole_Enum().GetValue(contributor?.ContributorRole?.Value), productId);
                    await AddSpecificationAttributeAsync("Person name", contributor?.AuthorName?.Value ?? "", productId);
                    await AddSpecificationAttributeAsync("Biographical Note", contributor?.BiographicalNote?.Value ?? "", productId);
                }
                break;

            case List<AncillaryContent> ancillaryContents:
                foreach (var ancillaryContent in ancillaryContents)
                {
                    //await AddSpecificationAttributeAsync("Ancillary content/Type", new OE_AncillaryContentType_Enum().GetValue(ancillaryContent?.AncillaryContentType?.Value) ?? "", productId);
                    await AddSpecificationAttributeAsync("Number of illustrations", ancillaryContent?.Number?.Value ?? "", productId);
                }
                break;

            case List<ProductSupply> productSupplies:
                foreach (var productSupply in productSupplies)
                {
                    foreach (var supplyDetails in productSupply.SupplyDetail)
                    {
                        await AddSpecificationAttributeAsync("Product Availability", supplyDetails?.ProductAvailability?.Value ?? "", productId);
                        foreach (var supplier in supplyDetails.Supplier)
                        {
                            await AddSpecificationAttributeAsync("Supplier Name", supplier?.SupplierName?.Value ?? "", productId);
                        }
                    }
                }
                break;

            case List<Prize> prize:
                foreach (var p in prize)
                    await AddSpecificationAttributeAsync("Prize", p?.PrizeName?.Value, productId, showOnProductPage: true);
                break;

            case List<ProductIdentifier> productIdentifiers:
                foreach (var productIdentifier in productIdentifiers)
                {

                    if (new OE_ProductIdentifierType_Enum().GetKeys("Proprietary").Contains(productIdentifier.ProductIDType.Value) && productIdentifier?.IDTypeName?.Value == "Title Code")
                    {
                        await AddSpecificationAttributeAsync("Title Code", productIdentifier?.IDValue?.Value ?? "", productId);
                        await CreateOrJoinParent(productId, productIdentifier?.IDValue?.Value ?? "");
                    }
                    else
                        await AddSpecificationAttributeAsync(new OE_ProductIdentifierType_Enum().GetValue(productIdentifier.ProductIDType.Value), productIdentifier?.IDValue?.Value ?? "", productId);


                }

                break;
        }
    }

    private async Task InsertOrUpdatePublishingStatusAsync(CatalogueProductsResponse onixProduct, int productId)
    {
        var columName = "Publishing Status";

        var specificationAttribute = await _specificationAttributeOptionsService.GetSpecificationAttributeByName(columName);

        string publishStatusValue = (new OE_PublishingStatus_Enum().GetKeys("Active").Contains(onixProduct.SortFields.PublishingStatus)).ToString();

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
        //var onixKeywords = response.DescriptiveDetail.Subject.Where(x => new OE_SubjectSchemeIdentifier_Enum().GetKeys("Keywords").Contains(x.SubjectSchemeIdentifier.Value));

        Dictionary<string, string> schemeIdentifiers = new();

        foreach (var subject in response.DescriptiveDetail.Subject)
        {
            if (subject is { SubjectHeadingText: not null })
            {
                var name = new OE_SubjectSchemeIdentifier_Enum().GetValue(subject.SubjectSchemeIdentifier.Value);
                var subjectHeadingText = HttpUtility.HtmlDecode(subject.SubjectHeadingText.Value);

                if (schemeIdentifiers.ContainsKey(name))
                    schemeIdentifiers[name] += ";" + subjectHeadingText;
                else
                    schemeIdentifiers[name] = subjectHeadingText;
            }
        }

        return schemeIdentifiers;
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
            specificationAttributeOptionId = (await _specificationAttributeOptionsService.GetSpecificationAttributeOptionIdByNameBySpecificationAttributeIdAsync(columnValue, specificationAttributeId)).Id;
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

    private async Task CreateOrJoinParent(int childProductId, string parentUniquekey)
    {
        if (string.IsNullOrWhiteSpace(parentUniquekey))
            return;

        var childProduct = await _productService.GetProductByIdAsync(childProductId);
        if (childProduct == null)
            return;

        var childPic = (await _pictureService.GetPicturesByProductIdAsync(childProductId)).FirstOrDefault();
        var parentProduct = _productRepository.Table.FirstOrDefault(x => x.AdminComment == parentUniquekey.ToLowerInvariant() && x.ProductTypeId == (int)ProductType.GroupedProduct && x.Deleted == false);

        if (parentProduct == null)
        {
            var productGroupTemplateId = (await _productTemplateService.GetAllProductTemplatesAsync()).FirstOrDefault(x => x.ViewPath == "ProductTemplate.Grouped").Id;

            parentProduct = new Product
            {
                Name = Regex.Replace(childProduct.Name, @"\s*\(.*\)", ""),
                FullDescription = childProduct.FullDescription,
                ShortDescription = childProduct.ShortDescription,
                Sku = "",
                ProductType = ProductType.GroupedProduct,
                ProductTypeId = (int)ProductType.GroupedProduct,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                AdminComment = parentUniquekey.ToLowerInvariant(),
                VisibleIndividually = true,
                Published = true,
                ProductTemplateId = productGroupTemplateId,
            };

            await _productService.InsertProductAsync(parentProduct);
            var seName = await _urlRecordService.ValidateSeNameAsync(parentProduct, null, parentProduct.Name, true);
            await _urlRecordService.SaveSlugAsync(parentProduct, seName, 0);
        }

        var parentPicId = (await _pictureService.GetPicturesByProductIdAsync(parentProduct.Id)).FirstOrDefault();
        if (childPic != null && parentPicId == null)
        {
            string invalidChars = @"[<>:""/\\|?*]";
            var seoName = Regex.Replace(parentProduct.Name, invalidChars, "");
            seoName = seoName.ToLower().Replace(" ", "_");

            var pictureBinary = await _pictureService.GetPictureBinaryByPictureIdAsync(childPic.Id);
            var duplicateBookPicture = await _pictureService.InsertPictureAsync(pictureBinary.BinaryData, MimeTypes.ImageJpeg, seoName);

            await InsertProductPictureAsync(duplicateBookPicture.Id, parentProduct.Id);
        }

        childProduct.ParentGroupedProductId = parentProduct.Id;
        childProduct.AdminComment = parentUniquekey.ToLowerInvariant();
        await _productService.UpdateProductAsync(childProduct);
    }

}