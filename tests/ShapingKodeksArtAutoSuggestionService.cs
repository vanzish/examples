using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Action.Platform.Logging.Interfaces;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces.Elastics;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces.ShapingSuggestions;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces.SuggestionRunningHistories;
using Action.Search.SuggestionService.ApplicationServices.Services.Interfaces.Suggestions;
using Action.Search.SuggestionService.ApplicationServices.Services.ShapingSuggestions.Base;
using Action.Search.SuggestionService.Data.Repositories.Interfaces.Dictionaries;
using Action.Search.SuggestionService.Data.Repositories.Interfaces.LawWeights;
using Action.Search.SuggestionService.Data.Repositories.Interfaces.SettingsRomNumbers;
using Action.Search.SuggestionService.Data.Repositories.Interfaces.SettingsTemplateDocNames;
using Action.Search.SuggestionService.Domain;
using Action.Search.SuggestionService.Domain.Configurations.Interfaces;
using Action.Search.SuggestionService.Domain.Dto;
using Action.Search.SuggestionService.Domain.EF;
using Action.Search.SuggestionService.Domain.EF.Suggestions;
using Action.Search.SuggestionService.Domain.ElasticDocuments.FederalLawArt;
using Action.Search.SuggestionService.Domain.ElasticDocuments.KodeksArtAuto;
using Action.Search.SuggestionService.Domain.Enums;
using Action.Search.SuggestionService.Kafka.Producers.Interfaces.Analyze;

using Common.Extensions;

using Nest;

using SimpleInjector;


namespace Action.Search.SuggestionService.ApplicationServices.Services.ShapingSuggestions.KodeksArtAuto
{
    /// <inheritdoc cref="IShapingKodeksArtAutoSuggestionService"/>
    internal class ShapingKodeksArtAutoSuggestionService
        : BaseShapingSuggestionsService<KodeksArtAutoDocument, SuggestionKodeksArt, IAllHashSuggestionService<SuggestionKodeksArt>>,
          IShapingKodeksArtAutoSuggestionService

    {
        private readonly IGetLawWeightAllRepository _getLawWeightAllRepository;

        private readonly IGetDateNowService _getDateNow;

        private readonly IKodeksArtAutoFullConfiguration _kodeksArtAutoFullConfiguration;

        private readonly IGetSettingsRomNumberAllRepository _settingsRomNumberAllRepository;

        private readonly IGetSettingsTemplateDocNameAllRepository _settingsTemplateDocNameAllRepository;

        private readonly IGetDictionaryAllRepository _getDictionaryAllRepository;

        private IEnumerable<LawWeight> _lawWeights;

        private IEnumerable<SettingsRomNumber> _romNumbers;

        private IEnumerable<SettingsTemplateDocName> _templateDocNames;

        private IEnumerable<Dictionary> _dictionaries;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="producer"></param>
        /// <param name="logger"></param>
        /// <param name="getLawWeightAllRepository"></param>
        /// <param name="getDateNow"></param>
        /// <param name="kodeksArtAutoFullConfiguration"></param>
        /// <param name="elasticWrapperClient"></param>
        /// <param name="settingsRomNumberAllRepository"></param>
        /// <param name="settingsTemplateDocNameAllRepository"></param>
        /// <param name="getDictionaryAllRepository"></param>
        /// <param name="container"></param>
        /// <param name="updateSuggestionRunningHistoryService"></param>
        /// <param name="suggestionRunningHistoryCheckCountService"></param>
        /// <param name="finishSuggestionRunningHistoryService"></param>
        public ShapingKodeksArtAutoSuggestionService(ISendSuggestionKodeksArtAutoToAnalyzeProducer producer,
                                                     ILoggingService logger,
                                                     IGetLawWeightAllRepository getLawWeightAllRepository,
                                                     IGetDateNowService getDateNow,
                                                     IKodeksArtAutoFullConfiguration kodeksArtAutoFullConfiguration,
                                                     IElasticWrapperClient elasticWrapperClient,
                                                     IGetSettingsRomNumberAllRepository settingsRomNumberAllRepository,
                                                     IGetSettingsTemplateDocNameAllRepository settingsTemplateDocNameAllRepository,
                                                     IGetDictionaryAllRepository getDictionaryAllRepository,
                                                     Container container,
                                                     IUpdateSuggestionRunningHistoryService updateSuggestionRunningHistoryService,
                                                     ISuggestionRunningHistoryCheckCountService suggestionRunningHistoryCheckCountService,
                                                     IFinishSuggestionRunningHistoryService finishSuggestionRunningHistoryService)
            : base(elasticWrapperClient, producer, logger, container, updateSuggestionRunningHistoryService, suggestionRunningHistoryCheckCountService, finishSuggestionRunningHistoryService)
        {
            _getLawWeightAllRepository = getLawWeightAllRepository;
            _getDateNow = getDateNow;
            _kodeksArtAutoFullConfiguration = kodeksArtAutoFullConfiguration;
            _settingsRomNumberAllRepository = settingsRomNumberAllRepository;
            _settingsTemplateDocNameAllRepository = settingsTemplateDocNameAllRepository;
            _getDictionaryAllRepository = getDictionaryAllRepository;
        }

        /// <summary>
        /// тип процедуры
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected override EProcedureType ProcedureType => EProcedureType.KodeksArt;

        private protected override SearchRequest GetSearchRequest()
        {
            var indices = Indices.Parse("prod_npd");
            var requestBuilder = new GetKodeksArtAutoRequestBuilder(_getDateNow);
            var request = requestBuilder.GetRequest(indices, _lawWeights, _kodeksArtAutoFullConfiguration.ScrollBatchSize);

            return request;
        }

        private protected override async Task<int> SendBatchToProducer(IEnumerable<KodeksArtAutoDocument> documents)
        {
            var suggestions = GetSuggestions(documents)
                .ToList();

            await UpdateSuggestionRunningHistoryService.Handle(new UpdateSuggestionRunningHistoryDto(ProcedureType, EModifiedFieldHistory.SendedToAnalyzeModified, suggestions.Count), CancellationToken.None);

            Logger.LogInfo($"Продюсер получил пачку документов на отправку на анализ в размере {suggestions.Count}");
            await Producer.SendToProducer(ProcedureType, EConsumerType.SendToAnalyze, suggestions);

            return suggestions.Count;
        }

        private protected override IEnumerable<SuggestionKodeksArt> GetSuggestions(IEnumerable<KodeksArtAutoDocument> documents)
        {
            var documentsWithTocs = documents
                                    .SelectMany(q => q.HeaderAnchors.Select(s => new FederalLawArtDto
                                                                                 {
                                                                                     ModuleId = q.ModuleId,
                                                                                     DocId = q.DocId,
                                                                                     GroupId = q.GroupId,
                                                                                     Type = s.Type,
                                                                                     SpecialNumbering = s.SpecialNumbering,
                                                                                     Anchor = s.Anchor
                                                                                 }))
                                    .ToList();

            var documentsWithChange = documentsWithTocs
                                      .Join(_romNumbers,
                                            s => s.SpecialNumbering?.ToLower(),
                                            r => r.RomanNumeral?.ToLower(),
                                            (s,
                                             r) => new FederalLawArtDto
                                                   {
                                                       ModuleId = s.ModuleId,
                                                       DocId = s.DocId,
                                                       GroupId = s.GroupId,
                                                       Type = s.Type,
                                                       SpecialNumbering = r.ArabicNumeral.ToString(),
                                                       Anchor = s.Anchor
                                                   })
                                      .ToList();

            documentsWithTocs.AddRange(documentsWithChange);

            var documentsWithRomAndArabic = documentsWithTocs
                                            .Where(q => !string.IsNullOrWhiteSpace(q.SpecialNumbering) && Regex.IsMatch(q.SpecialNumbering, "[0-9]") && Regex.IsMatch(q.SpecialNumbering.ToLower(), "[ivxlc]"))
                                            .ToList();

            foreach (var one in documentsWithRomAndArabic)
            {
                var symbol = Regex.Replace(one.SpecialNumbering.ToLower(), "[^ivxlc]", "");
                var arabic = _romNumbers.FirstOrDefault(q => string.Equals(q.RomanNumeral, symbol, StringComparison.OrdinalIgnoreCase));

                if (arabic != null)
                    one.SpecialNumbering = one.SpecialNumbering.ToLower()
                                              .Replace(symbol, arabic.ArabicNumeral.ToString());
            }

            documentsWithTocs.AddRange(documentsWithRomAndArabic);


            var suggestions = documentsWithTocs
                              .Join(_lawWeights,
                                    header => header.GroupId,
                                    lawWeight => lawWeight.GroupId,
                                    (header,
                                     lawWeight) => new
                                                   {
                                                       lawWeight.PubId,
                                                       lawWeight.RequestCount,
                                                       lawWeight.GroupId,
                                                       FormattedLongName = lawWeight.LongName.FirstLetterToUpper(),
                                                       lawWeight.ShortName,
                                                       lawWeight.LongName,
                                                       header.SpecialNumbering,
                                                       header.Anchor,
                                                       header.Type,
                                                       header.ModuleId,
                                                       header.DocId,
                                                   })
                              .Join(_templateDocNames,
                                    header => header.Type.ToUpper(),
                                    template => template.Alias.ToUpper(),
                                    (header,
                                     template) =>
                                    {
                                        var docName = template.Value
                                                              .Replace("%NUMBER%", header.SpecialNumbering)
                                                              .Replace("%LONGNAME%", header.FormattedLongName);

                                        return new
                                               {
                                                   DocName = docName,
                                                   template.DocTypeId,
                                                   header.PubId,
                                                   header.RequestCount,
                                                   header.GroupId,
                                                   header.ShortName,
                                                   header.LongName,
                                                   header.SpecialNumbering,
                                                   header.Anchor,
                                                   header.Type,
                                                   header.ModuleId,
                                                   header.DocId,
                                               };
                                    })
                              .Join(_dictionaries,
                                    header => header.DocTypeId,
                                    dictionary => dictionary.TypeId,
                                    (header,
                                     dictionary) =>
                                    {
                                        var docName =
                                            dictionary.Name
                                                      .Replace("%NUMBER%", header.SpecialNumbering)
                                                      .Replace("%LONGNAME%", header.LongName)
                                                      .Replace("%SHORTNAME%", header.ShortName);

                                        var result = new SuggestionKodeksArt
                                                     {
                                                         Id = Guid.NewGuid(),
                                                         PubId = header.PubId,
                                                         PubDivId = 0,
                                                         SearchRequest = docName,
                                                         RequestCount = header.RequestCount,
                                                         SuggestionTypeId = _kodeksArtAutoFullConfiguration.SuggestionTypeId,
                                                         ModuleId = header.ModuleId,
                                                         DocId = header.DocId,
                                                         Anchor = header.Anchor,
                                                         DocName = header.DocName,
                                                         GroupId = header.GroupId,
                                                         NavigateTypeId = 2,
                                                     };

                                        if (result.SearchRequest.IndexOf('.') != -1)
                                        {
                                            result.RequestCount -= result.SearchRequest.Count(x => x == '.');
                                        }

                                        return result;
                                    }).ToList();

            suggestions = suggestions.Distinct(new KodeksArtAutoEqualityComparer()).ToList();

            return suggestions;
        }

        private protected override async Task SetSettings(CancellationToken cancellationToken)
        {
            _lawWeights = await _getLawWeightAllRepository.GetAsync(Unit.Default, cancellationToken);
            _romNumbers = await _settingsRomNumberAllRepository.GetAsync(Unit.Default, cancellationToken);
            _templateDocNames = (await _settingsTemplateDocNameAllRepository.GetAsync(Unit.Default, cancellationToken)).Where(q => q.SuggestionType == 4);
            _dictionaries = await _getDictionaryAllRepository.GetAsync(Unit.Default, cancellationToken);
        }
    }
}