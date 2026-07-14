using Confluent.Kafka;
using CreditCardApi.Application.Transactions.Dtos;
using CreditCardApi.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CreditCardApi.UnitTests.Infrastructure;

public class KafkaTransactionEventPublisherTests
{
    private readonly Mock<IProducer<string, string>> _producer = new();
    private readonly KafkaTransactionEventPublisher _sut;

    private static readonly TransactionResponse SampleTransaction = new(1, 1, 199.90m, "Amazon", "shopping", DateTime.UtcNow);

    public KafkaTransactionEventPublisherTests()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "kafka:9092", TransactionsTopic = "transactions" });
        _sut = new KafkaTransactionEventPublisher(_producer.Object, options, NullLogger<KafkaTransactionEventPublisher>.Instance);
    }

    [Fact]
    public async Task PublishCreatedAsync_WhenProduceSucceeds_SendsExactlyOneMessage()
    {
        _producer
            .Setup(p => p.ProduceAsync("transactions", It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string> { Topic = "transactions" });

        await _sut.PublishCreatedAsync(SampleTransaction, CancellationToken.None);

        _producer.Verify(
            p => p.ProduceAsync("transactions", It.Is<Message<string, string>>(m => m.Key == "1"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishCreatedAsync_WhenTheBrokerIsUnreachable_RetriesThenSwallowsTheFailure()
    {
        _producer
            .Setup(p => p.ProduceAsync("transactions", It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProduceException<string, string>(
                new Error(ErrorCode.Local_Transport, "broker unreachable"),
                new DeliveryResult<string, string> { Topic = "transactions" }));

        // The whole point of this test: a broker outage must never throw out of PublishCreatedAsync,
        // since the transaction row is already committed by the time this is called.
        await _sut.PublishCreatedAsync(SampleTransaction, CancellationToken.None);

        // 1 initial attempt + 3 configured retries.
        _producer.Verify(
            p => p.ProduceAsync("transactions", It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    [Fact]
    public async Task PublishCreatedAsync_SerializesTheTransactionAsCamelCaseJson()
    {
        Message<string, string>? capturedMessage = null;
        _producer
            .Setup(p => p.ProduceAsync("transactions", It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((_, message, _) => capturedMessage = message)
            .ReturnsAsync(new DeliveryResult<string, string> { Topic = "transactions" });

        await _sut.PublishCreatedAsync(SampleTransaction, CancellationToken.None);

        Assert.NotNull(capturedMessage);
        Assert.Contains("\"creditCardId\"", capturedMessage.Value, StringComparison.Ordinal);
        Assert.Contains("\"merchant\":\"Amazon\"", capturedMessage.Value, StringComparison.Ordinal);
    }
}
