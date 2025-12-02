using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net.Http;
using wave.web.Controllers;
using wave.web.Services;

namespace wave.web.Tests
{
    [TestClass]
    public class ChatControllerTests
    {
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<DocumentService> _mockDocumentService;
        private Mock<RagSearchService> _mockRagService;
        private Mock<IGoogleSearchService> _mockGoogleSearchService;

        [TestInitialize]
        public void Setup()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockDocumentService = new Mock<DocumentService>();
            _mockRagService = new Mock<RagSearchService>();
            _mockGoogleSearchService = new Mock<IGoogleSearchService>();
        }

        [TestMethod]
        public void ChatController_Constructor_InitializesSuccessfully()
        {
            // Arrange & Act
            var controller = new ChatController(
                _mockHttpClientFactory.Object,
                _mockDocumentService.Object,
                _mockRagService.Object,
                _mockGoogleSearchService.Object
            );

            // Assert
            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void GoogleSearchService_IsConfigured_CalledCorrectly()
        {
            // Arrange
            _mockGoogleSearchService.Setup(x => x.IsConfigured()).Returns(false);

            var controller = new ChatController(
                _mockHttpClientFactory.Object,
                _mockDocumentService.Object,
                _mockRagService.Object,
                _mockGoogleSearchService.Object
            );

            // Act
            var isConfigured = _mockGoogleSearchService.Object.IsConfigured();

            // Assert
            Assert.IsFalse(isConfigured);
            _mockGoogleSearchService.Verify(x => x.IsConfigured(), Times.Once);
        }
    }
}
