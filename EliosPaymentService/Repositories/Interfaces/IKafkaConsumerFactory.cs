using EliosPaymentService.Repositories.Interfaces;

namespace CEliosPaymentService.Repositories.Interfaces
{
    public interface IKafkaConsumerFactory<T> where T : class
    {
        IKafkaConsumerRepository<T> CreateConsumer(string sourceServiceName);
    }
}