

namespace EliosPaymentService.Repositories.Interfaces
{
    public interface IAppConfiguration
    {
        string GetKafkaBootstrapServers();

        string GetCurrentServiceName();
    }
}
