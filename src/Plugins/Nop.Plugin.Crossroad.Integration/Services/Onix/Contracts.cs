using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
        [property: JsonPropertyName("Measure")] List<Measure> Measure,
        [property: JsonPropertyName("ProductFormDetail")] List<ProductFormDetail> ProductFormDetail,
        [property: JsonPropertyName("Subject")] List<Subject> Subject);


    #region Langugage

    public record Language([property: JsonPropertyName("LanguageCode")] LanguageCode LanguageCode);

    public record LanguageCode([property: JsonPropertyName("Value")] string Language);

    #endregion

    #region Extent

    public record Extent([property: JsonPropertyName("ExtentValue")] ExtentValue NumberOfPages);

    public record ExtentValue([property: JsonPropertyName("Value")] string Number);

    #endregion

    #region Measures

    public record Measure([property: JsonPropertyName("MeasureType")] MeasureType Type,
        [property: JsonPropertyName("Measurement")] Measurement Measurement);

    public record MeasureType([property: JsonPropertyName("Value")] string Value);
    public record Measurement([property: JsonPropertyName("Value")] string Measure);

    #endregion

    #region Subjects

    public record Subject([property: JsonPropertyName("SubjectSchemeName")] SubjectSchemeName SchemeName,
        [property: JsonPropertyName("SubjectHeadingText")] SubjectHeadingText Keywords);

    public record SubjectSchemeName([property: JsonPropertyName("Value")] string Name);

    public record SubjectHeadingText([property: JsonPropertyName("Value")] string KeywordsValues);

    #endregion

    #region Text

    public record CollateralDetail([property: JsonPropertyName("TextContent")] List<TextContent> TextContent);

    public record TextContent([property: JsonPropertyName("TextType")] TextType TextType,
        [property: JsonPropertyName("Text")] Text Text);

    public record TextType([property: JsonPropertyName("Value")] string Type);

    public record Text([property: JsonPropertyName("Value")] string Description);

    #endregion

    #region Product details

    public record ProductFormDetail([property: JsonPropertyName("Value")] string ProductFormCode);

    public record ProductSupply([property: JsonPropertyName("SupplyDetail")] List<SupplyDetail> SupplyDetail);

    public record SupplyDetail([property: JsonPropertyName("Price")] List<Price> Price);

    public record Price([property: JsonPropertyName("PriceAmount")] PriceAmount PriceAmount);

    public record PriceAmount([property: JsonPropertyName("Value")] string Price);

    #endregion

    #region Publish deatils

    public record PublishingDetail([property: JsonPropertyName("Imprint")] List<Imprint> Imprint);

    public record Imprint([property: JsonPropertyName("ImprintName")] ImprintName ImprintName);

    public record ImprintName([property: JsonPropertyName("Value")] string Name);

    #endregion

    #region Contributor

    public record Contributor([property: JsonPropertyName("ContributorRole")] ContributorRole ContributorRole,
        [property: JsonPropertyName("PersonName")] PersonName AuthorName,
        [property: JsonPropertyName("PersonNameInverted")] PersonNameInverted AuthorNameInverted);

    public record ContributorRole([property: JsonPropertyName("Value")] string Value);

    public record PersonName([property: JsonPropertyName("Value")] string Value);

    public record PersonNameInverted([property: JsonPropertyName("Value")] string Value);


    #endregion

    #endregion

    public record SortFields([property: JsonPropertyName("ISBN13")] string ISBN13,
                             [property: JsonPropertyName("Title")] string Title,
                             [property: JsonPropertyName("CoverImage")] string CoverImage,
                             [property: JsonPropertyName("PublicationDate")] DateTime? PublicationDate,
                             [property: JsonPropertyName("PublishingStatus")] string PublishingStatus);


    public record ProductPrices(string Name, Dictionary<string, ProductSkus> Prices);

    public record ProductSkus(string Sku, decimal Prices);
}