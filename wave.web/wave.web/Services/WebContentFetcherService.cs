using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlParser;
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
            var nodes = Parser.Parse(html, loadContent: true);
            
            // Find main content areas - prefer article or body
            INode? contentNode = null;
            foreach (var node in nodes)
            {
                contentNode = FindNode(node, NodeType.article) 
                           ?? FindNode(node, NodeType.body);
                if (contentNode != null) break;
            }

            if (contentNode == null)
            {
                return string.Empty;
            }

            var text = GetTextContent(contentNode);

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

        private INode? FindNode(INode node, NodeType nodeType)
        {
            if (node == null) return null;
            if (node.Type == nodeType) return node;
            
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var found = FindNode(child, nodeType);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private string GetTextContent(INode node)
        {
            if (node == null || node.Children == null) return string.Empty;
            
            // Skip certain tags
            if (node.Type == NodeType.script || node.Type == NodeType.style || 
                node.Type == NodeType.nav || node.Type == NodeType.header || 
                node.Type == NodeType.footer || node.Type == NodeType.aside ||
                node.Type == NodeType.iframe || node.Type == NodeType.noscript)
            {
                return string.Empty;
            }
            
            // Return content for text nodes
            if (node.Type == NodeType.text)
            {
                return node.Content ?? string.Empty;
            }
            
            // Recursively get text from children
            var sb = new StringBuilder();
            foreach (var child in node.Children)
            {
                sb.Append(GetTextContent(child));
                sb.Append(" ");
            }
            return sb.ToString();
        }
    }
}
