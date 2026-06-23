namespace CreditCardApi.Infrastructure;

public interface IKafkaProducer
{
    Task PublishTransactionAsync(string key, string message);
}
