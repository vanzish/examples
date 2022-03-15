using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Action.Platform.Common.Exceptions;
using Action.Platform.Logging.Interfaces;
using Action.Search.FactoidService.Core.Configuration.Kafka.Interfaces;
using Action.Search.FactoidService.Core.LoggerExtensions;
using Confluent.Kafka;
using Kafka.MessageBroker;


namespace Action.Search.FactoidService.Kafka.Consumers.Abstractions
{
    /// <summary>
    /// Базовый класс для консьюмеров.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public abstract class BaseConsumer<T> : MessageConsumer<string, T>, IBaseConsumer<T>
    {
        private readonly IKafkaConfiguration _config;

        private readonly ILoggingService _logger;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <exception cref="BrokenRulesException"></exception>
        public BaseConsumer(IKafkaConfiguration config, ILoggingService logger)
        {
            _config = config;
            _logger = logger;

            _logger.LogInfo($"Значение brokerList из конфигов: {Config.BrokerListString}");
            BrokerList = string.Join(",", Config.BrokerList);

            if (string.IsNullOrWhiteSpace(BrokerList))
                throw new BrokenRulesException($"BrokerList is Empty. Consumer");

            TopicList = new List<string>
            {
                Topic
            };

            GroupId = ConsumerGroupId;
            CustomSettings = config.Settings;
            ConsumeIntervalMs = 500;
            IsOnMessageAsync = true;
            DocConsumeCount = 25;
        }

        /// <summary>
        /// Топик
        /// </summary>
        protected abstract string Topic { get; }

        /// <summary>
        /// группа
        /// </summary>
        protected abstract string ConsumerGroupId { get; }

        /// <summary>
        /// Конфигурация.
        /// </summary>
        public IKafkaConfiguration Config => _config;

        /// <summary>
        /// Логгер.
        /// </summary>
        public ILoggingService Logger => _logger;

        /// <inheritdoc />
        protected override void OnConsumeError(Message<string, T> msg)
        {
            _logger.LogError($"Ошибка приёма сообщения");
        }

        /// <inheritdoc />
        protected override void OnError(Error error)
        {
            _logger.LogError($"Ошибка консьюмера {error.Code}: {error.Reason}");
        }

        /// <inheritdoc />
        protected override void OnPartitionsAssigned(List<TopicPartition> partitions)
        {
            var message = string.Join(" ", partitions.Select(p => $"{p.Topic}[{p.Partition}]"));

            _logger.LogInfo($"Partitions assigned: {message}");
        }

        /// <inheritdoc />
        protected override void OnPartitionsRevoked(List<TopicPartitionOffset> partitions)
        {
            _logger.LogInfo($"Partitions revoked");
        }
    }
}