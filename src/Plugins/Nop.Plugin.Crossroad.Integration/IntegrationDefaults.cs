using Nop.Core.Caching;

namespace Nop.Plugin.Crossroad.Integration;

public class IntegrationDefaults
{
    public static CacheKey TokenExpirationCacheKey => new("CROSSROAD_INTEGRATION_TOKEN_EXPIRATION");

    public static CacheKey AccessTokenCacheKey => new("CROSSROAD_INTEGRATION_ACCESS_TOKEN");

    public static string BOOK_WIDTH_KEY = "05";

    public static string BOOK_HEIGHT_KEY = "04";
    
    public static string BOOK_REVIEWS_KEY = "06";

    public static string BOOK_TABLE_OF_CONTENT_KEY = "04";

    public static string BOOK_DESCRIPTION_KEY = "03";

    public static string BOOK_PUBLISH_STATUS_KEY = "04";

    public static string BOOK_ENDORSMENT_KEY = "09";

    public const string Paperback = "B102";

    public const string Hardcover = "B409";

    public const string Epub = "E101";

    public const string Audio_MP3  = "A103";

    public const string Audio_CD  = "A101";

    public const string ProductAttributeColumnName = "Type";
}