using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml.Office2010.Ink;
using Svg;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Nop.Plugin.Crossroad.Integration.Services.Onix;

public class Contracts
{
    #region Login

    public record LoginRequest(string Username, string Password);
    public record LoginResponse([property: JsonPropertyName("expires")] int Expires,
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("data_source_type")] string DataSourceType,
        [property: JsonPropertyName("tokenGUID")] string TokenGuid);

    #endregion

    #region Catalogues

    public record CatalogueResponse([property: JsonPropertyName("ID")] int Id);


    public record CatalogueProductsResponse([property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("SortFields")] SortFields SortFields,
        [property: JsonPropertyName("DescriptiveDetail")] DescriptiveDetail DescriptiveDetail,
        [property: JsonPropertyName("CollateralDetail")] CollateralDetail CollateralDetail,
        [property: JsonPropertyName("PublishingDetail")] PublishingDetail PublishingDetail,
        [property: JsonPropertyName("ProductSupply")] List<ProductSupply> ProductSupply);

    #endregion

    #region Descriptive details

    public record DescriptiveDetail([property: JsonPropertyName("Contributor")] List<Contributor> Contributor,
        [property: JsonPropertyName("Language")] List<Language> Language,
        [property: JsonPropertyName("Extent")] List<Extent> Extent,
        [property: JsonPropertyName("EditionNumber")] EditionNumber EditionNumber,
        [property: JsonPropertyName("EditionStatement")] EditionStatement EditionStatement,
        [property: JsonPropertyName("AncillaryContent")] List<AncillaryContent> AncillaryContent,
        [property: JsonPropertyName("Measure")] List<Measure> Measure,
        [property: JsonPropertyName("ProductFormDetail")] List<ProductFormDetail> ProductFormDetail,
        [property: JsonPropertyName("Subject")] List<Subject> Subject,
        [property: JsonPropertyName("TitleDetail")] List<TitleDetail> TitleDetail);


    #region Langugage

    public record Language([property: JsonPropertyName("LanguageCode")] LanguageCode LanguageCode);

    public record LanguageCode([property: JsonPropertyName("Value")] string Language);

    #endregion

    #region Extent

    public record Extent([property: JsonPropertyName("ExtentType")] ExtentType ExtentType,
        [property: JsonPropertyName("ExtentValue")] ExtentValue ExtentValue,
        [property: JsonPropertyName("ExtentUnit")] ExtentUnit ExtentUnit);

    public record ExtentType([property: JsonPropertyName("Value")] string Value);
    public record ExtentValue([property: JsonPropertyName("Value")] string Value);
    public record ExtentUnit([property: JsonPropertyName("Value")] string Value);

    #endregion

    #region EditionNumber

    public record EditionNumber([property: JsonPropertyName("Value")] string Value);

    #endregion

    #region EditionStatement

    public record EditionStatement([property: JsonPropertyName("Value")] string Value);

    #endregion

    #region AncillaryContent

    public record AncillaryContent([property: JsonPropertyName("AncillaryContentType")] AncillaryContentType AncillaryContentType,
        [property: JsonPropertyName("Number")] Number Number);

    public record AncillaryContentType([property: JsonPropertyName("Value")] string Value);
    public record Number([property: JsonPropertyName("Value")] string Value);

    #endregion

    #region Measures

    public record Measure([property: JsonPropertyName("MeasureType")] MeasureType Type,
        [property: JsonPropertyName("Measurement")] Measurement Measurement);

    public record MeasureType([property: JsonPropertyName("Value")] string Value);
    public record Measurement([property: JsonPropertyName("Value")] string Measure);

    #endregion

    #region Subjects 23/34/25

    public record Subject([property: JsonPropertyName("SubjectSchemeIdentifier")] SubjectSchemeIdentifier SubjectSchemeIdentifier,
        [property: JsonPropertyName("SubjectSchemeName")] SubjectSchemeName SchemeName,
        [property: JsonPropertyName("SubjectHeadingText")] SubjectHeadingText Keywords);

    public record SubjectSchemeIdentifier([property: JsonPropertyName("Value")] string Value);
    public record SubjectSchemeName([property: JsonPropertyName("Value")] string Name);

    public record SubjectHeadingText([property: JsonPropertyName("Value")] string KeywordsValues);

    #endregion

    #region TitleDetail
    public record TitleDetail([property: JsonPropertyName("TitleElement")] List<TitleElement> TitleElement);
    public record TitleElement([property: JsonPropertyName("TitlePrefix")] TitlePrefix TitlePrefix,
        [property: JsonPropertyName("TitleWithoutPrefix")] TitleWithoutPrefix TitleWithoutPrefix,
        [property: JsonPropertyName("Subtitle")] Subtitle Subtitle);
    public record TitlePrefix([property: JsonPropertyName("Value")] string TitlePrefixValue);
    public record TitleWithoutPrefix([property: JsonPropertyName("Value")] string TitleWithoutPrefixValue);
    public record Subtitle([property: JsonPropertyName("Value")] string SubtitleValue);

    #endregion

    #region Text

    public record CollateralDetail([property: JsonPropertyName("TextContent")] List<TextContent> TextContent,
        [property: JsonPropertyName("SupportingResource")] List<SupportingResource> SupportingResource);

    public record TextContent([property: JsonPropertyName("TextType")] TextType TextType,
        [property: JsonPropertyName("Text")] Text Text);

    public record TextType([property: JsonPropertyName("Value")] string Type);

    public record Text([property: JsonPropertyName("Value")] string Description);

    public record SupportingResource([property: JsonPropertyName("ResourceFeature")] List<ResourceFeature> ResourceFeature,
        [property: JsonPropertyName("ResourceVersion")] List<ResourceVersion> ResourceVersion);

    public record ResourceFeature([property: JsonPropertyName("ResourceFeatureType")] ResourceFeatureType ResourceFeatureType,
        [property: JsonPropertyName("FeatureNote")] FeatureNote FeatureNote);

    public record ResourceFeatureType([property: JsonPropertyName("Value")] string Value);

    public record FeatureNote([property: JsonPropertyName("Value")] string Value);
    public record ResourceVersion([property: JsonPropertyName("ResourceLink")] ResourceLink ResourceLink);

    public record ResourceLink([property: JsonPropertyName("Value")] string Value);

    #endregion

    #region Product details

    public record ProductFormDetail([property: JsonPropertyName("Value")] string ProductFormCode);

    public record ProductSupply([property: JsonPropertyName("SupplyDetail")] List<SupplyDetail> SupplyDetail);

    public record SupplyDetail([property: JsonPropertyName("Supplier")] List<Supplier> Supplier,
        [property: JsonPropertyName("ProductAvailability")] ProductAvailability ProductAvailability,
        [property: JsonPropertyName("Price")] List<Price> Price);


    public record Supplier([property: JsonPropertyName("SupplierName")] SupplierName SupplierName);

    public record SupplierName([property: JsonPropertyName("Value")] string Value);

    public record ProductAvailability([property: JsonPropertyName("Value")] string Value);

    public record Price([property: JsonPropertyName("PriceAmount")] PriceAmount PriceAmount);
    public record PriceAmount([property: JsonPropertyName("Value")] string Price);

    #endregion

    #region Publish deatils

    public record PublishingDetail([property: JsonPropertyName("Imprint")] List<Imprint> Imprint,
        [property: JsonPropertyName("PublishingDate")] List<PublishingDate> PublishingDate);

    public record Imprint([property: JsonPropertyName("ImprintName")] ImprintName ImprintName);

    public record ImprintName([property: JsonPropertyName("Value")] string Name);

    public record PublishingDate([property: JsonPropertyName("PublishingDateRole")] PublishingDateRole PublishingDateRole,
        [property: JsonPropertyName("Date")] Date Date);

    public record PublishingDateRole([property: JsonPropertyName("Value")] string Value);
    public record Date([property: JsonPropertyName("ISODate")] DateTime? ISODate);

    #endregion

    #region Contributor

    public record Contributor([property: JsonPropertyName("ContributorRole")] ContributorRole ContributorRole,
        [property: JsonPropertyName("PersonName")] PersonName AuthorName,
        [property: JsonPropertyName("PersonNameInverted")] PersonNameInverted AuthorNameInverted,
        [property: JsonPropertyName("BiographicalNote")] BiographicalNote BiographicalNote);

    public record ContributorRole([property: JsonPropertyName("Value")] string Value);

    public record PersonName([property: JsonPropertyName("Value")] string Value);

    public record PersonNameInverted([property: JsonPropertyName("Value")] string Value);

    public record BiographicalNote([property: JsonPropertyName("Value")] string Value);


    #endregion

    #endregion

    public record SortFields([property: JsonPropertyName("ISBN13")] string ISBN13,
                             [property: JsonPropertyName("ISBN10")] string ISBN10,
                             [property: JsonPropertyName("Title")] string Title,
                             [property: JsonPropertyName("CoverImage")] string CoverImage,
                             [property: JsonPropertyName("PublicationDate")] DateTime? PublicationDate,
                             [property: JsonPropertyName("PublishingStatus")] string PublishingStatus);


    public record ProductPrices(string Name, Dictionary<string, ProductSkus> Prices);

    public record ProductSkus(string Sku, decimal Prices);
}