using System.Threading.Tasks;
using Gemini.Models;

namespace Gemini.Messaging;

public interface IKafkaProducer
{
    Task PublishTransactionAsync(Transaction transaction);
}
