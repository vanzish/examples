using System;
using System.Linq;
using System.Threading.Tasks;
using Action.Search.DocumentCollectorService.ApplicationServices.Helpers;
using Action.Search.DocumentCollectorService.ApplicationServices.Services.Interfaces;
using Action.Search.DocumentCollectorService.ApplicationServices.Settings.Interfaces;
using Action.Search.DocumentCollectorService.Core.Entities.Enums;
using Action.Search.DocumentCollectorService.Core.IndexDocuments.Interfaces;
using SimpleInjector;


namespace Action.Search.DocumentCollectorService.ApplicationServices.IndexDocuments.AttributeSetters
{
    /// <summary>
    /// Декоратор статистики
    /// </summary>
    public class StatisticsAttributesSetterDecorator : BaseAttributesSetterDecorator
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="indexDocumentAttributesSetter"></param>
        /// <param name="container"></param>
        public StatisticsAttributesSetterDecorator(BaseAttributeSetter indexDocumentAttributesSetter, Container container)
            : base(indexDocumentAttributesSetter, container)
        {
        }

        /// <inheritdoc cref="BaseAttributesSetterDecorator.SetDocumentAttributes"/>
        public override async Task SetDocumentAttributes(ELanguage language)
        {
            if (!(_component.IndexDocument is IPubStatisticsDocument statisticsDocument))
            {
                throw new Exception($"невозможно установить атрибуты статистики для документа не реализующего интерфейс {nameof(IPubStatisticsDocument)}");
            }

            await using var scope = new Scope(Container);

            var getAllPubsService = scope.GetInstance<IGetAllPubsService>();
            var pubs = await getAllPubsService.GetAllPubsAsync();

            var statisticService = scope.GetInstance<IStatisticService>();
            var statistics = await statisticService.GetDocumentPubStatistic(_component.IndexDocument.ModuleId, _component.IndexDocument.DocId, pubs, language);

            var maxPubId = pubs.Max(x => x.Id);
            var popular = IndexValueHelper.GetDoubleListFromPubValueList(maxPubId,
                                                                         statistics,
                                                                         (pubId, list) =>
                                                                         {
                                                                             var statistic = statistics.FirstOrDefault(f => f.PubId == pubId);

                                                                             return statistic?.Popular;
                                                                         });

            statisticsDocument.PubPopular = popular;

            // Set Attributes
            await base.SetDocumentAttributes(language);
        }
    }
}