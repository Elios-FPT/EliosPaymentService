using CEliosPaymentService.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EliosPaymentService.Services
{
    public class KafkaConsumerHostedService<T> : BackgroundService where T : class
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly string _sourceServiceName;

        public KafkaConsumerHostedService(
            IServiceScopeFactory serviceScopeFactory,
            string sourceServiceName)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _sourceServiceName = sourceServiceName ?? throw new ArgumentNullException(nameof(sourceServiceName));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            await Task.Run(async () =>
            {
                try
                {
                    var scope = _serviceScopeFactory.CreateScope();
                    var _consumerFactory = scope.ServiceProvider.GetRequiredService<IKafkaConsumerFactory<T>>();
                    var kafkaConsumer = _consumerFactory.CreateConsumer(_sourceServiceName);
                    await kafkaConsumer.StartConsumingAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }, stoppingToken);
        }
    }

}
