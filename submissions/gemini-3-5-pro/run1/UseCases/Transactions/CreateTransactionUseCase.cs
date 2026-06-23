using System;
using System.Threading.Tasks;
using CreditCardApi.Domain;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;
using CreditCardApi.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;

namespace CreditCardApi.UseCases.Transactions
{
    public class CreateTransactionUseCase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IKafkaProducerService _kafkaProducerService;
        private readonly string _topicName;

        public CreateTransactionUseCase(
            ITransactionRepository transactionRepository,
            ICreditCardRepository creditCardRepository,
            IKafkaProducerService kafkaProducerService,
            IConfiguration configuration)
        {
            _transactionRepository = transactionRepository;
            _creditCardRepository = creditCardRepository;
            _kafkaProducerService = kafkaProducerService;
            _topicName = configuration.GetValue<string>("Kafka:TransactionsTopic") ?? "transactions";
        }

        public async Task<UseCaseResult<TransactionResponse>> ExecuteAsync(CreateTransactionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Merchant))
            {
                return UseCaseResult<TransactionResponse>.Fail("Merchant is required and cannot be empty.");
            }

            if (request.Amount <= 0)
            {
                return UseCaseResult<TransactionResponse>.Fail("Amount must be greater than 0.");
            }

            var card = await _creditCardRepository.GetByIdAsync(request.CreditCardId);
            if (card == null)
            {
                return UseCaseResult<TransactionResponse>.Fail("Credit card does not exist.");
            }

            var transaction = new Transaction
            {
                CreditCardId = request.CreditCardId,
                Amount = request.Amount,
                Merchant = request.Merchant.Trim(),
                Category = request.Category?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(transaction);

            var response = new TransactionResponse
            {
                Id = transaction.Id,
                CreditCardId = transaction.CreditCardId,
                Amount = transaction.Amount,
                Merchant = transaction.Merchant,
                Category = transaction.Category,
                CreatedAt = transaction.CreatedAt
            };

            // Publish to Kafka (orchestrated in the use case per requirements)
            await _kafkaProducerService.PublishTransactionCreatedAsync(_topicName, response);

            return UseCaseResult<TransactionResponse>.Ok(response);
        }
    }
}
