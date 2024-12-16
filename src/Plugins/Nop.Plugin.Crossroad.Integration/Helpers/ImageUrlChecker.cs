using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Nop.Plugin.Crossroad.Integration.Helpers
{
    public class ImageUrlChecker
    {
        public List<string> ImageUrls = new List<string>();

        private bool AreUrlsReferToSameImage(string url1, string url2)
        {
            string baseUrl1 = GetBaseUrl(url1);
            string baseUrl2 = GetBaseUrl(url2);

            if (baseUrl1 == baseUrl2)
                return true;

            return false;
        }

        private string GetBaseUrl(string url)
        {
            url = url.Replace("_thumb.", ".");
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

        public string AddUrlToDictionary(string url)
        {
            bool isUnique = true;

            foreach (var entry in ImageUrls)
            {
                if (AreUrlsReferToSameImage(entry, url))
                {
                    isUnique = false;
                    break;
                }
            }

            string baseUrl = GetBaseUrl(url);
            if (isUnique && !string.IsNullOrWhiteSpace(baseUrl) && !ImageUrls.Contains(baseUrl))
            {
                ImageUrls.Add(baseUrl);
                return baseUrl;
            }
            else
                return "";
        }

    }
}
