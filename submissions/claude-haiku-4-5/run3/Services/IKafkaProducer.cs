namespace CreditCardApi.Services;

public interface IKafkaProducer
{
    Task PublishTransactionAsync(string message, string key);
}
