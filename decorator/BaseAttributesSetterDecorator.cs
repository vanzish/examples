using System.Threading.Tasks;
using Action.Search.DocumentCollectorService.Core.Entities.Enums;
using SimpleInjector;


namespace Action.Search.DocumentCollectorService.ApplicationServices.IndexDocuments.AttributeSetters
{
    /// <summary>
    /// Базовый декоратор
    /// </summary>
    public class BaseAttributesSetterDecorator : BaseAttributeSetter
    {
        /// <summary>
        /// Сущность объекта для выполнения предыдущей обертки
        /// </summary>
        protected BaseAttributeSetter _component;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="component"></param>
        /// <param name="container"></param>
        public BaseAttributesSetterDecorator(BaseAttributeSetter component, Container container)
            : base(component.IndexDocument, container)
        {
            _component = component;
        }

        /// <inheritdoc cref="BaseAttributeSetter.SetDocumentAttributes"/>
        public override async Task SetDocumentAttributes(ELanguage language)
        {
            if (_component != null)
            {
                await _component.SetDocumentAttributes(language);
            }
        }
    }
}