using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CreditCardApi.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CreditCardApi.UnitTests.Infrastructure;

public class KafkaTopicInitializerTests
{
    private readonly Mock<IAdminClient> _adminClient = new();
    private readonly KafkaTopicInitializer _sut;

    public KafkaTopicInitializerTests()
    {
        var options = Options.Create(new KafkaOptions { TransactionsTopic = "transactions" });
        _sut = new KafkaTopicInitializer(options, NullLogger<KafkaTopicInitializer>.Instance);
    }

    [Fact]
    public async Task CreateTopicIfMissingAsync_WhenTheTopicDoesNotExist_CreatesIt()
    {
        _adminClient
            .Setup(c => c.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), null))
            .Returns(Task.CompletedTask);

        await _sut.CreateTopicIfMissingAsync(_adminClient.Object, "transactions");

        _adminClient.Verify(
            c => c.CreateTopicsAsync(It.Is<IEnumerable<TopicSpecification>>(specs => specs.Single().Name == "transactions"), null),
            Times.Once);
    }

    [Fact]
    public async Task CreateTopicIfMissingAsync_WhenTheTopicAlreadyExists_SwallowsTheException()
    {
        var alreadyExistsResult = new CreateTopicReport { Topic = "transactions", Error = new Error(ErrorCode.TopicAlreadyExists) };
        _adminClient
            .Setup(c => c.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), null))
            .ThrowsAsync(new CreateTopicsException([alreadyExistsResult]));

        await _sut.CreateTopicIfMissingAsync(_adminClient.Object, "transactions");
    }

    [Fact]
    public async Task CreateTopicIfMissingAsync_WhenCreationFailsForAnotherReason_Rethrows()
    {
        var failureResult = new CreateTopicReport { Topic = "transactions", Error = new Error(ErrorCode.BrokerNotAvailable) };
        _adminClient
            .Setup(c => c.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), null))
            .ThrowsAsync(new CreateTopicsException([failureResult]));

        await Assert.ThrowsAsync<CreateTopicsException>(
            () => _sut.CreateTopicIfMissingAsync(_adminClient.Object, "transactions"));
    }
}
