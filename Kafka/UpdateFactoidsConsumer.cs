using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Action.Platform.Logging.Interfaces;
using Action.Search.FactoidService.ApplicationServices.Services.Interfaces;
using Action.Search.FactoidService.Core.Configuration.Kafka.Interfaces;
using Action.Search.FactoidService.Core.LoggerExtensions;
using Action.Search.FactoidService.Kafka.Consumers.Abstractions;
using Action.Search.FactoidService.Kafka.Consumers.Interfaces;
using AutoMapper;
using Common.Models.Dto;
using Common.Models.Kafka;
using Confluent.Kafka;


namespace Action.Search.FactoidService.Kafka.Consumers
{
    /// <inheritdoc cref="IUpdateFactoidsConsumer"/>
    [ExcludeFromCodeCoverage]
    public class UpdateFactoidsConsumer : BaseConsumer<List<FactoidData>>, IUpdateFactoidsConsumer
    {
        private readonly IUpdateFactoidsService _updateFactoidsService;
        private readonly IMapper _mapper;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="updateFactoidsService"></param>
        /// <param name="mapper"></param>
        public UpdateFactoidsConsumer(
            IKafkaConfiguration configuration,
            ILoggingService logger,
            IUpdateFactoidsService updateFactoidsService,
            IMapper mapper)
            : base(configuration, logger)
        {
            _updateFactoidsService = updateFactoidsService;
            _mapper = mapper;
        }

        /// <inheritdoc/>
        protected override string Topic => Config.UpdateFactoidTopic;

        /// <inheritdoc/>
        protected override string ConsumerGroupId => Config.UpdateFactoidGroupId;

        /// <inheritdoc />
        protected override async Task OnMessageAsync(Message<string, List<FactoidData>> msg, string topic)
        {
            if (msg.Value != null && msg.Value.Any())
            {
                try
                {
                    Logger.LogInfo($"Принята пачка фактоидов на обновление. Количество: {msg.Value.Count}");

                    var dtos = _mapper.Map<List<FactoidDto>>(msg.Value);
                    await _updateFactoidsService.UpdateFactoidsAsync(dtos);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Неизвестная ошибка при обработке документа {ex.ErrorMessage()}");
                }
            }
            else
            {
                Logger.LogError($"Получено пустое сообщение из топика {topic}");
            }
        }
    }
}