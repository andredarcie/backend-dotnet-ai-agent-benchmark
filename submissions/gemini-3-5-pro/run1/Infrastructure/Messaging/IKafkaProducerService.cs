using System.Threading.Tasks;

namespace CreditCardApi.Infrastructure.Messaging
{
    public interface IKafkaProducerService
    {
        Task PublishTransactionCreatedAsync(string topic, object message);
    }
}
