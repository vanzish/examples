using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Action.Platform.Logging.Interfaces;
using Action.Platform.Logging.Models;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces.ShapingSuggestions;
using Action.Search.SuggestionService.ApplicationServices.Services.ShapingSuggestions.KodeksArtAuto;
using Action.Search.SuggestionService.Domain;
using Action.Search.SuggestionService.Domain.EF.Suggestions;
using Action.Search.SuggestionService.Domain.ElasticDocuments.KodeksArtAuto;

using FluentAssertions;

using Moq;

using Tests.Fakes.FakeServices.ShapingSuggestions;

using Xunit;


namespace Tests.ApplicationServiceTests.Services.ShapingSuggestions
{
    public class ShapingKodeksArtAutoAutoSuggestionServiceTests
    {
        [Fact]
        public async Task Handle_WithEmptyResult_ShouldBeEmpty()
        {
            // Arrange
            var logger = new Mock<ILoggingService>();
            var service = new FakeShapingKodeksArtAutoSuggestionServiceFactory().GetService_WithReturnEmpty(logger.Object);

            // Act
            Func<Task> result = async () => await service.Handle(Unit.Default, CancellationToken.None);

            // Assert
            await result.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Handle_WithOneItem_ShouldBeSendOneItem()
        {
            // Arrange
            var logger = new Mock<ILoggingService>();
            var service = new FakeShapingKodeksArtAutoSuggestionServiceFactory().GetService_WithOneItem(logger.Object);

            // Act
            Func<Task> result = async () => await service.Handle(Unit.Default, CancellationToken.None);

            // Assert
            await result.Should().NotThrowAsync();
        }
        
        [Fact]
        public async Task Handle_WithOneItem_EmptyHeaders_ShouldBeSendOneItem()
        {
            // Arrange
            var logger = new Mock<ILoggingService>();
            var service = new FakeShapingKodeksArtAutoSuggestionServiceFactory().GetService_WithOneItem_EmptyHeader(logger.Object);

            // Act
            Func<Task> result = async () => await service.Handle(Unit.Default, CancellationToken.None);

            // Assert
            await result.Should().NotThrowAsync();
        }
        [Fact]
        public async Task Handle_WithExceptionInSearchAsync_ShouldException()
        {
            // Arrange
            var logger = new Mock<ILoggingService>();
            var service = new FakeShapingKodeksArtAutoSuggestionServiceFactory().GetService_WithValidFalseInSearch(logger.Object);

            // Act
            await service.Handle(Unit.Default, CancellationToken.None);

            // Assert
            logger.Verify(s=>s.Log(LoggingParameters.Error, It.Is<string>(x => x.StartsWith("Ошибка при получении подсказок: Ошибка при поиске в эластик")), It.IsAny<IDictionary<string,string>>(), It.IsAny<IDictionary<string,string>>()), Times.Once);
        }
        
        [Fact]
        public void GetSuggestions_CheckChangeRomanNumber_ShouldBeCorrect()
        {
            // Arrange
            var logger = new Mock<ILoggingService>();

            var doc = new KodeksArtAutoDocument
                      {
                          DocId = 1,
                          GroupId = 1,
                          HeaderAnchors = new List<KodeksArtAutoHeaderAnchors>
                                          {
                                              new KodeksArtAutoHeaderAnchors
                                              {
                                                  Type = "section",
                                                  Anchor = "anchor",
                                                  SpecialNumbering = "V"
                                              }
                                          }
                      };
            var service = new FakeShapingKodeksArtAutoSuggestionServiceFactory().GetService_WithOneItem(logger.Object);
            
            // Act
            var result = ExecuteGetSuggestions(service,
                                               new List<KodeksArtAutoDocument>
                                               {
                                                   doc
                                               }).ToList();
            Assert.Contains(result,s=> s.SearchRequest == "раздел 5 test");
            Assert.Contains(result, q => q.DocName == "Раздел 5 Test");
        }
        
        [Fact]
        public void GetSuggestions_CheckChangeRomanNumberWithArabic_ShouldBeCorrect()
        {
            // Arrange
            var logger = new Mock<ILoggingService>();

            var doc = new KodeksArtAutoDocument
                      {
                          DocId = 1,
                          GroupId = 1,
                          HeaderAnchors = new List<KodeksArtAutoHeaderAnchors>
                                          {
                                              new KodeksArtAutoHeaderAnchors
                                              {
                                                  Type = "section",
                                                  Anchor = "anchor",
                                                  SpecialNumbering = "V.5"
                                              }
                                          }
                      };
            var service = new FakeShapingKodeksArtAutoSuggestionServiceFactory().GetService_WithOneItem(logger.Object);
            
            // Act
            var result = ExecuteGetSuggestions(service,
                                               new List<KodeksArtAutoDocument>
                                               {
                                                   doc
                                               }).ToList();
            Assert.Contains(result,s=> s.SearchRequest == "раздел 5.5 test");
            Assert.Contains(result, q => q.DocName == "Раздел 5.5 Test");
        }
        
        
        [Fact]
        public void GetSuggestions_CheckChangeRomanNumberWithArabic_ShouldBeCorrect2()
        {
            // Arrange
            var logger = new Mock<ILoggingService>();

            var doc = new KodeksArtAutoDocument
                      {
                          DocId = 1,
                          GroupId = 1,
                          HeaderAnchors = new List<KodeksArtAutoHeaderAnchors>
                                          {
                                              new KodeksArtAutoHeaderAnchors
                                              {
                                                  Type = "section",
                                                  Anchor = "anchor",
                                                  SpecialNumbering = "II.5"
                                              }
                                          }
                      };
            var service = new FakeShapingKodeksArtAutoSuggestionServiceFactory().GetService_WithOneItem(logger.Object);
            
            // Act
            var result = ExecuteGetSuggestions(service,
                                               new List<KodeksArtAutoDocument>
                                               {
                                                   doc
                                               }).ToList();
            Assert.Contains(result,s=> s.SearchRequest == "раздел 2.5 test");
            Assert.Contains(result, q => q.DocName == "Раздел 2.5 Test");
        }
        
        private IEnumerable<SuggestionKodeksArt> ExecuteGetSuggestions(
            IShapingKodeksArtAutoSuggestionService service,
            List<KodeksArtAutoDocument> request)
        {
            var type = typeof(ShapingKodeksArtAutoSuggestionService);
            var methodSettings = type
                                 .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                                 .First(x => x.Name == "SetSettings" );
            methodSettings.Invoke(service, new object[] {CancellationToken.None});
            
            var method = type
                         .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                         .First(x => x.Name == "GetSuggestions" );
            var res = (List<SuggestionKodeksArt>)method.Invoke(service, new object[] {request});
            return res;
        }
    }
}