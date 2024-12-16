using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Nop.Plugin.Crossroad.Integration.Helpers
{
    public class ImageUrlChecker
    {
        public static Dictionary<string, string> ImageUrls = new Dictionary<string, string>();

        private static bool AreUrlsReferToSameImage(string url1, string url2)
        {
            string baseUrl1 = GetBaseUrl(url1);
            string baseUrl2 = GetBaseUrl(url2);

            if (baseUrl1 != baseUrl2)
            {
                return false;
            }


            string filename1 = GetFileName(url1);
            string filename2 = GetFileName(url2);

            if (filename1 == filename2 || AreThumbnailVariants(filename1, filename2))
            {
                return true;
            }

            return false;
        }

        private static string GetBaseUrl(string url)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                int lastSlashIndex = url.LastIndexOf('/');
                return url.Substring(0, lastSlashIndex);
            }
            else
            {
                return "";
            }
        }

        private static string GetFileName(string url)
        {
            int lastSlashIndex = url.LastIndexOf('/');
            var filename = url.Substring(lastSlashIndex + 1);
            filename = Path.GetFileNameWithoutExtension(filename);
            filename = Regex.Replace(filename, "_[^_]*$", "");
            return filename;
        }

        private static bool AreThumbnailVariants(string filename1, string filename2)
        {

            if (filename1.Contains("_thumb") && filename2.Contains(".jpg") && filename1.Replace("_thumb", "") == filename2)
            {
                return true;
            }
            if (filename2.Contains("_thumb") && filename1.Contains(".jpg") && filename2.Replace("_thumb", "") == filename1)
            {
                return true;
            }
            return false;
        }

        public static string AddUrlToDictionary(string url)
        {
            bool isUnique = true;

            foreach (var entry in ImageUrls)
            {
                if (AreUrlsReferToSameImage(entry.Key, url))
                {
                    isUnique = false;
                    break;
                }
            }

            string baseUrl = GetBaseUrl(url);
            string filename = GetFileName(url);
            if (isUnique && !string.IsNullOrWhiteSpace(baseUrl) && !ImageUrls.ContainsKey(baseUrl + filename))
            {
                ImageUrls[baseUrl + "/" + filename] = filename;
                return baseUrl + "/" + filename + ".jpg";
            }
            else
                return "";
        }

    }
}
