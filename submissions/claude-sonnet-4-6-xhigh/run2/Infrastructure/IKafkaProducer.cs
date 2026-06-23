namespace CreditCardApi.Infrastructure;

public interface IKafkaProducer
{
    Task PublishAsync(string topic, string key, string value);
}
