using System.Threading;
using System.Threading.Tasks;

namespace CreditCardApi.Application.Common.Interfaces;

/// <summary>
/// Interface for publishing events to the messaging broker.
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Publishes a message to the specified topic.
    /// </summary>
    Task PublishAsync(string topic, string key, string value, CancellationToken cancellationToken = default);
}
