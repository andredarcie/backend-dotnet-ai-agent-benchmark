using CreditCardApi.Models;

namespace CreditCardApi.Services;

public interface IKafkaProducerService
{
    Task PublishTransactionAsync(Transaction transaction);
}
