using System.Threading.Tasks;
using Action.Search.DocumentCollectorService.ApplicationServices.IndexDocuments.Interfaces;
using Action.Search.DocumentCollectorService.Core.Entities.Enums;
using Action.Search.DocumentCollectorService.Core.IndexDocuments.Entities;
using SimpleInjector;


namespace Action.Search.DocumentCollectorService.ApplicationServices.IndexDocuments.AttributeSetters
{
    /// <inheritdoc cref="IIndexDocumentAttributesSetter"/>
    public class BaseAttributeSetter : IIndexDocumentAttributesSetter
    {
        private readonly Container _container;

        /// <summary>
        /// Document
        /// </summary>
        public BaseIndexDocument IndexDocument { get; set; }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="indexDocument"></param>
        /// <param name="container"></param>
        public BaseAttributeSetter(BaseIndexDocument indexDocument, Container container)
        {
            _container = container;
            IndexDocument = indexDocument;
        }

        /// <summary>
        /// Контейнер
        /// </summary>
        protected Container Container => _container;

        /// <inheritdoc cref="IIndexDocumentAttributesSetter.SetDocumentAttributes"/>
        public virtual  Task SetDocumentAttributes(ELanguage language)
        {
            // Do nothing
            return Task.CompletedTask;
        }
    }
}