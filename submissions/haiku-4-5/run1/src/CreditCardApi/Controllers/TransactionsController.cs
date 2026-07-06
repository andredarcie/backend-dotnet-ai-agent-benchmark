using CreditCardApi.Application.DTOs;
using CreditCardApi.Application.Repositories;
using CreditCardApi.Domain.Entities;
using CreditCardApi.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CreditCardApi.Controllers;

/// <summary>
/// Transactions API endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TransactionsController(ITransactionRepository repository, ICreditCardRepository cardRepository, IKafkaProducer kafkaProducer) : ControllerBase
{
    /// <summary>
    /// Get all transactions (paginated).
    /// </summary>
    /// <param name="pageNumber">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of transactions.</returns>
    [HttpGet]
    [ProduceResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var transactions = await repository.GetAllAsync(pageNumber, pageSize, cancellationToken);
        var dtos = transactions.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Get a transaction by ID.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction.</returns>
    [HttpGet("{id}")]
    [ProduceResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetByIdAsync(id, cancellationToken);
        if (transaction == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} not found."
            });
        }

        return Ok(MapToDto(transaction));
    }

    /// <summary>
    /// Create a new transaction.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transaction.</returns>
    [HttpPost]
    [ProduceResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Merchant))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Merchant name is required and cannot be empty."
            });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Amount must be greater than 0."
            });
        }

        var creditCard = await cardRepository.GetByIdAsync(request.CreditCardId, cancellationToken);
        if (creditCard == null)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Credit card with ID {request.CreditCardId} does not exist."
            });
        }

        var transaction = new Transaction
        {
            CreditCardId = request.CreditCardId,
            Amount = request.Amount,
            Merchant = request.Merchant,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        var createdTransaction = await repository.CreateAsync(transaction, cancellationToken);

        try
        {
            var transactionDto = MapToDto(createdTransaction);
            var json = JsonSerializer.Serialize(transactionDto, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await kafkaProducer.PublishAsync("transactions", createdTransaction.Id.ToString(), json, cancellationToken);
        }
        catch (Exception ex)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<TransactionsController>>();
            logger.LogError(ex, "Failed to publish transaction created event to Kafka");
        }

        var dto = MapToDto(createdTransaction);
        return CreatedAtAction(nameof(GetById), new { id = createdTransaction.Id }, dto);
    }

    /// <summary>
    /// Update a transaction.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transaction.</returns>
    [HttpPut("{id}")]
    [ProduceResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProduceResponseType(StatusCodes.Status204NoContent)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var existingTransaction = await repository.GetByIdAsync(id, cancellationToken);
        if (existingTransaction == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} not found."
            });
        }

        if (request.Merchant != null && string.IsNullOrWhiteSpace(request.Merchant))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Merchant name cannot be empty."
            });
        }

        if (request.Amount.HasValue && request.Amount <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Amount must be greater than 0."
            });
        }

        if (request.CreditCardId.HasValue)
        {
            var creditCard = await cardRepository.GetByIdAsync(request.CreditCardId.Value, cancellationToken);
            if (creditCard == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Type = "https://example.com/errors/validation",
                    Title = "Validation Error",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Credit card with ID {request.CreditCardId} does not exist."
                });
            }
        }

        var transactionToUpdate = new Transaction
        {
            CreditCardId = request.CreditCardId ?? existingTransaction.CreditCardId,
            Amount = request.Amount ?? existingTransaction.Amount,
            Merchant = request.Merchant ?? existingTransaction.Merchant,
            Category = request.Category ?? existingTransaction.Category
        };

        var updatedTransaction = await repository.UpdateAsync(id, transactionToUpdate, cancellationToken);
        if (updatedTransaction == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(updatedTransaction));
    }

    /// <summary>
    /// Delete a transaction.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id}")]
    [ProduceResponseType(StatusCodes.Status204NoContent)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} not found."
            });
        }

        return NoContent();
    }

    private static TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt
        };
    }
}
