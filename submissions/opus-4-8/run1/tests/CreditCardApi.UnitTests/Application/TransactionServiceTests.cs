using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Domain.Entities;
using CreditCardApi.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CreditCardApi.UnitTests.Application;

public sealed class TransactionServiceTests
{
    [Fact]
    public async Task CreateAsync_with_unknown_card_throws_validation()
    {
        var service = new TransactionService(
            new FakeTransactionRepository(),
            new FakeCreditCardRepository(),
            new FakeUnitOfWork(),
            new RecordingEventPublisher(),
            new FixedClock());

        var act = () => service.CreateAsync(
            new CreateTransactionRequest { CreditCardId = 99, Amount = 10m, Merchant = "Amazon" },
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainValidationException>())
            .Which.Field.Should().Be("CreditCardId");
    }

    [Fact]
    public async Task CreateAsync_persists_transaction_and_enqueues_event()
    {
        var transactions = new FakeTransactionRepository();
        var events = new RecordingEventPublisher();
        var service = new TransactionService(
            transactions,
            new FakeCreditCardRepository(1),
            new FakeUnitOfWork(),
            events,
            new FixedClock());

        var response = await service.CreateAsync(
            new CreateTransactionRequest { CreditCardId = 1, Amount = 199.90m, Merchant = "Amazon", Category = "shopping" },
            CancellationToken.None);

        response.Merchant.Should().Be("Amazon");
        response.Amount.Should().Be(199.90m);
        transactions.Added.Should().ContainSingle();

        events.Events.Should().ContainSingle();
        events.Events[0].Topic.Should().Be(TransactionService.Topic);
        events.Events[0].EventType.Should().Be(TransactionService.TransactionCreatedEvent);
    }

    private sealed class FakeCreditCardRepository : ICreditCardRepository
    {
        private readonly HashSet<int> _ids;

        public FakeCreditCardRepository(params int[] ids) => _ids = [.. ids];

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_ids.Contains(id));

        public Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<CreditCard?>(null);

        public Task<IReadOnlyList<CreditCard>> ListAsync(int skip, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CreditCard>>([]);

        public Task<int> CountAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public void Add(CreditCard card)
        {
        }

        public void Remove(CreditCard card)
        {
        }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public List<Transaction> Added { get; } = [];

        public void Add(Transaction transaction) => Added.Add(transaction);

        public void Remove(Transaction transaction) => Added.Remove(transaction);

        public Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(Added.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<Transaction>> ListAsync(int skip, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Transaction>>(Added);

        public Task<int> CountAsync(CancellationToken cancellationToken) => Task.FromResult(Added.Count);

        public Task<IReadOnlyList<Transaction>> ListByCardAsync(int creditCardId, int skip, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Transaction>>(Added.Where(t => t.CreditCardId == creditCardId).ToList());

        public Task<int> CountByCardAsync(int creditCardId, CancellationToken cancellationToken) =>
            Task.FromResult(Added.Count(t => t.CreditCardId == creditCardId));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }

    private sealed class RecordingEventPublisher : IIntegrationEventPublisher
    {
        public List<(string Topic, string Key, string EventType, object Payload)> Events { get; } = [];

        public void Enqueue(string topic, string key, string eventType, object payload) =>
            Events.Add((topic, key, eventType, payload));
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
