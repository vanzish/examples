using System.Collections.Generic;
using System.Threading;

using Action.Platform.Logging.Interfaces;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces.Elastics;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces.ShapingSuggestions;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces.SuggestionRunningHistories;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces.Suggestions;
using Action.Search.SuggestionService.ApplicationServices.Services.ShapingSuggestions.KodeksArtAuto;
using Action.Search.SuggestionService.Domain.Dto;
using Action.Search.SuggestionService.Domain.EF.Suggestions;
using Action.Search.SuggestionService.Domain.ElasticDocuments.KodeksArtAuto;
using Action.Search.SuggestionService.Domain.Enums;
using Action.Search.SuggestionService.Domain.Responses.Elastics;

using Moq;

using Nest;

using SimpleInjector;

using Tests.Fakes.FakeConfigurations;
using Tests.Fakes.FakeProducers;
using Tests.Fakes.FakeRepositories.Dictionaries;
using Tests.Fakes.FakeRepositories.LawWeights;
using Tests.Fakes.FakeRepositories.SettingsRomNumbers;
using Tests.Fakes.FakeRepositories.SettingsTemplateDocNames;
using Tests.Fakes.FakeServices.SuggestionRunningHistories;


namespace Tests.Fakes.FakeServices.ShapingSuggestions
{
    public class FakeShapingKodeksArtAutoSuggestionServiceFactory
    {
        public IShapingKodeksArtAutoSuggestionService GetService_WithReturnEmpty(ILoggingService logger = null)
        {
            var client = new Mock<IElasticWrapperClient>();

            client.Setup(s => s.SearchAsync<KodeksArtAutoDocument>(It.IsAny<SearchRequest>(), CancellationToken.None))
                  .ReturnsAsync(() => new SearchWrapperResponse<KodeksArtAutoDocument>
                                      {
                                          IsValid = true,
                                          Documents = new List<KodeksArtAutoDocument>(),
                                          HitsCount = 0
                                      });

            var service = GetService(client.Object, logger);

            return service;
        }

        public IShapingKodeksArtAutoSuggestionService GetService_WithValidFalseInSearch(ILoggingService logger = null)
        {
            var client = new Mock<IElasticWrapperClient>();

            client.Setup(s => s.SearchAsync<KodeksArtAutoDocument>(It.IsAny<SearchRequest>(), CancellationToken.None))
                  .ReturnsAsync(() => new SearchWrapperResponse<KodeksArtAutoDocument>
                                      {
                                          IsValid = false,
                                          Documents = new List<KodeksArtAutoDocument>(),
                                          HitsCount = 0
                                      });

            var service = GetService(client.Object, logger);

            return service;
        }

        public IShapingKodeksArtAutoSuggestionService GetService_WithOneItem(ILoggingService logger = null)
        {
            var client = new Mock<IElasticWrapperClient>();

            client.SetupSequence(s => s.SearchAsync<KodeksArtAutoDocument>(It.IsAny<SearchRequest>(), CancellationToken.None))
                  .ReturnsAsync(() => new SearchWrapperResponse<KodeksArtAutoDocument>
                                      {
                                          IsValid = true,
                                          Documents = new List<KodeksArtAutoDocument>(),
                                          HitsCount = 1
                                      });

            client.SetupSequence(s => s.ScrollAsync<KodeksArtAutoDocument>(It.IsAny<string>(), CancellationToken.None))
                  .ReturnsAsync(() => new SearchWrapperResponse<KodeksArtAutoDocument>
                                      {
                                          IsValid = true,
                                          Documents = new List<KodeksArtAutoDocument>
                                                      {
                                                          new KodeksArtAutoDocument
                                                          {
                                                              DocId = 1,
                                                              DocName = "Главное в декабре",
                                                              GroupId = 1,
                                                              ModuleId = 1,
                                                              HeaderAnchors = new List<KodeksArtAutoHeaderAnchors>
                                                                              {
                                                                                  new KodeksArtAutoHeaderAnchors
                                                                                  {
                                                                                      Anchor = "test",
                                                                                      Type = "section",
                                                                                      SpecialNumbering = "I"
                                                                                  },
                                                                                  new KodeksArtAutoHeaderAnchors
                                                                                  {
                                                                                      Anchor = "test",
                                                                                      Type = "section",
                                                                                      SpecialNumbering = "I.5"
                                                                                  }
                                                                              }
                                                          }
                                                      },
                                          HitsCount = 1
                                      })
                  .ReturnsAsync(() => new SearchWrapperResponse<KodeksArtAutoDocument>
                                      {
                                          IsValid = true,
                                          Documents = new List<KodeksArtAutoDocument>(),
                                          HitsCount = 0
                                      });

            var service = GetService(client.Object, logger);

            return service;
        }

        public IShapingKodeksArtAutoSuggestionService GetService_WithOneItem_EmptyHeader(ILoggingService logger = null)
        {
            var client = new Mock<IElasticWrapperClient>();

            client.SetupSequence(s => s.SearchAsync<KodeksArtAutoDocument>(It.IsAny<SearchRequest>(), CancellationToken.None))
                  .ReturnsAsync(() => new SearchWrapperResponse<KodeksArtAutoDocument>
                                      {
                                          IsValid = true,
                                          Documents = new List<KodeksArtAutoDocument>(),
                                          HitsCount = 1
                                      });

            client.SetupSequence(s => s.ScrollAsync<KodeksArtAutoDocument>(It.IsAny<string>(), CancellationToken.None))
                  .ReturnsAsync(() => new SearchWrapperResponse<KodeksArtAutoDocument>
                                      {
                                          IsValid = true,
                                          Documents = new List<KodeksArtAutoDocument>
                                                      {
                                                          new KodeksArtAutoDocument
                                                          {
                                                              DocId = 1,
                                                              DocName = "Главное в декабре",
                                                              GroupId = 1,
                                                              ModuleId = 1
                                                          }
                                                      },
                                          HitsCount = 1
                                      });

            var service = GetService(client.Object, logger);

            return service;
        }

        private IShapingKodeksArtAutoSuggestionService GetService(
            IElasticWrapperClient elasticWrapperClient,
            ILoggingService loggingService,
            IFinishSuggestionRunningHistoryService finishSuggestionRunningHistoryService = null)
        {
            if (finishSuggestionRunningHistoryService == null)
            {
                var mockService = new Mock<IFinishSuggestionRunningHistoryService>();
                mockService.Setup(s => s.Handle(It.IsAny<FinishSuggestionRunningHistoryDto>(), It.IsAny<CancellationToken>()));
                finishSuggestionRunningHistoryService = mockService.Object;
            }
            var container = new Container();

            var mockHashService = new Mock<IAllHashSuggestionService<SuggestionKodeksArt>>();
            mockHashService.Setup(x => x.LoadSuggestionHash(It.IsAny<CancellationToken>()));
            mockHashService.SetupGet(x => x.Suggestions).Returns(new List<DtoSuggestionHash>());

            container.RegisterInstance(mockHashService.Object);

            var service = new ShapingKodeksArtAutoSuggestionService(FakeSendSuggestionKodeksArtAutoToAnalyzeProducerFactory.GetProducer(),
                                                                    loggingService ?? FakeLoggingServiceFactory.GetLogger(),
                                                                    FakeGetLawWeightAllRepositoryFactory.GetRepository(),
                                                                    FakeGetDateNowServiceFactory.GetService(),
                                                                    FakeKodeksArtAutoFullConfigurationFactory.GetConfig(),
                                                                    elasticWrapperClient,
                                                                    FakeGetSettingsRomNumberAllRepositoryFactory.GetRepository_WithXAndV(),
                                                                    FakeGetSettingsTemplateDocNameAllRepositoryFactory.GetRepository(),
                                                                    FakeGetDictionaryAllRepositoryFactory.GetRepository(),
                                                                    container,
                                                                    new FakeUpdateSuggestionRunningHistoryServiceFactory().GetService_WithMockDb(EProcedureType.KodeksArt),
                                                                    new FakeSuggestionRunningHistoryCheckCountServiceFactory().GetService_WithMockWork(),
                                                                    finishSuggestionRunningHistoryService);

            return service;
        }
    }
}