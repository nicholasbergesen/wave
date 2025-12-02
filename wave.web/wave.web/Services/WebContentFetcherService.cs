using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace wave.web.Services
{
    public interface IWebContentFetcherService
    {
        Task<string?> FetchPageContentAsync(string url);
    }

    public class WebContentFetcherService : IWebContentFetcherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebContentFetcherService> _logger;

        public WebContentFetcherService(IHttpClientFactory httpClientFactory, ILogger<WebContentFetcherService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _logger = logger;
        }

        public async Task<string?> FetchPageContentAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch content from {Url}. Status: {Status}", url, response.StatusCode);
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();
                return ExtractTextFromHtml(html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching content from {Url}", url);
                return null;
            }
        }

        private string ExtractTextFromHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script, style, and other non-content elements
            var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//nav|//header|//footer|//aside|//iframe|//noscript");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                {
                    node.Remove();
                }
            }

            // Try to find main content area
            var contentNode = doc.DocumentNode.SelectSingleNode("//main") 
                           ?? doc.DocumentNode.SelectSingleNode("//article")
                           ?? doc.DocumentNode.SelectSingleNode("//div[@class='content']")
                           ?? doc.DocumentNode.SelectSingleNode("//div[@id='content']")
                           ?? doc.DocumentNode.SelectSingleNode("//body");

            if (contentNode == null)
            {
                return string.Empty;
            }

            var text = contentNode.InnerText;

            // Clean up the text
            text = Regex.Replace(text, @"\s+", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = text.Trim();

            // Limit content size to avoid overwhelming the model
            const int maxLength = 8000;
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength) + "...";
            }

            return text;
        }
    }
}
