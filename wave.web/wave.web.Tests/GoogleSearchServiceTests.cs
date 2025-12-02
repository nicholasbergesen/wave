using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using wave.web.Models;
using wave.web.Services;

namespace wave.web.Tests
{
    [TestClass]
    public class GoogleSearchServiceTests
    {
        private Mock<ILogger<GoogleSearchService>> _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<GoogleSearchService>>();
        }

        [TestMethod]
        public void IsConfigured_WithEmptyApiKey_ReturnsFalse()
        {
            // Arrange
            var options = Options.Create(new GoogleSearchOptions
            {
                ApiKey = "",
                SearchEngineId = "test-engine-id"
            });

            // Act
            using var service = new GoogleSearchService(options, _mockLogger.Object);

            // Assert
            Assert.IsFalse(service.IsConfigured());
        }

        [TestMethod]
        public void IsConfigured_WithEmptySearchEngineId_ReturnsFalse()
        {
            // Arrange
            var options = Options.Create(new GoogleSearchOptions
            {
                ApiKey = "test-api-key",
                SearchEngineId = ""
            });

            // Act
            using var service = new GoogleSearchService(options, _mockLogger.Object);

            // Assert
            Assert.IsFalse(service.IsConfigured());
        }

        [TestMethod]
        public void IsConfigured_WithBothValuesEmpty_ReturnsFalse()
        {
            // Arrange
            var options = Options.Create(new GoogleSearchOptions
            {
                ApiKey = "",
                SearchEngineId = ""
            });

            // Act
            using var service = new GoogleSearchService(options, _mockLogger.Object);

            // Assert
            Assert.IsFalse(service.IsConfigured());
        }

        [TestMethod]
        public async Task SearchAsync_WhenNotConfigured_ReturnsEmptyList()
        {
            // Arrange
            var options = Options.Create(new GoogleSearchOptions
            {
                ApiKey = "",
                SearchEngineId = ""
            });

            using var service = new GoogleSearchService(options, _mockLogger.Object);

            // Act
            var results = await service.SearchAsync("test query");

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Constructor_WithMissingConfiguration_LogsWarning()
        {
            // Arrange
            var options = Options.Create(new GoogleSearchOptions
            {
                ApiKey = "",
                SearchEngineId = ""
            });

            // Act
            using var service = new GoogleSearchService(options, _mockLogger.Object);

            // Assert - verify warning was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not configured")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
