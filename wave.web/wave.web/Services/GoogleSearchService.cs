using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using wave.web.Models;

namespace wave.web.Services
{
    public interface IGoogleSearchService
    {
        Task<List<WebSearchResult>> SearchAsync(string query, int maxResults = 5);
        bool IsConfigured();
    }

    public class GoogleSearchService : IGoogleSearchService, IDisposable
    {
        private readonly GoogleSearchOptions _options;
        private readonly CustomSearchAPIService? _searchService;
        private readonly ILogger<GoogleSearchService> _logger;

        public GoogleSearchService(IOptions<GoogleSearchOptions> options, ILogger<GoogleSearchService> logger)
        {
            _options = options.Value;
            _logger = logger;

            if (!string.IsNullOrEmpty(_options.ApiKey) && !string.IsNullOrEmpty(_options.SearchEngineId))
            {
                try
                {
                    _searchService = new CustomSearchAPIService(new BaseClientService.Initializer
                    {
                        ApiKey = _options.ApiKey,
                        ApplicationName = "Wave Chat Assistant"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Google Search Service");
                }
            }
            else
            {
                _logger.LogWarning("Google Search Service is not configured. API Key or Search Engine ID is missing.");
            }
        }

        public bool IsConfigured()
        {
            return _searchService != null;
        }

        public async Task<List<WebSearchResult>> SearchAsync(string query, int maxResults = 5)
        {
            if (_searchService == null)
            {
                _logger.LogWarning("Google Search Service is not configured. Cannot perform search.");
                return new List<WebSearchResult>();
            }

            try
            {
                var listRequest = _searchService.Cse.List();
                listRequest.Q = query;
                listRequest.Cx = _options.SearchEngineId;
                listRequest.Num = maxResults;

                var search = await listRequest.ExecuteAsync();
                
                if (search.Items == null || !search.Items.Any())
                {
                    _logger.LogInformation("No search results found for query: {Query}", query);
                    return new List<WebSearchResult>();
                }

                return search.Items.Select(item => new WebSearchResult
                {
                    Title = item.Title ?? string.Empty,
                    Link = item.Link ?? string.Empty,
                    Snippet = item.Snippet ?? string.Empty,
                    DisplayLink = item.DisplayLink ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing Google search for query: {Query}", query);
                return new List<WebSearchResult>();
            }
        }

        public void Dispose()
        {
            _searchService?.Dispose();
        }
    }
}
