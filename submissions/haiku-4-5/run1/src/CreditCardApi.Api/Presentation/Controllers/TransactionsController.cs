using CreditCardApi.Api.Application.Dto;
using CreditCardApi.Api.Domain.Entities;
using CreditCardApi.Api.Infrastructure.Data;
using CreditCardApi.Api.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Api.Presentation.Controllers;

[ApiController]
[Route("api/transactions")]
[Produces("application/json", "application/problem+json")]
public class TransactionsController(CreditCardDbContext context, ITransactionProducer producer) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TransactionResponse>>> GetTransactions(
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid pagination parameters",
                Status = StatusCodes.Status400BadRequest,
                Detail = "page and pageSize must be >= 1, and pageSize <= 100",
            });
        }

        var totalCount = await context.Transactions.CountAsync(cancellationToken);
        var transactions = await context.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(t => new TransactionResponse
            {
                Id = t.Id,
                CreditCardId = t.CreditCardId,
                Amount = t.Amount,
                Merchant = t.Merchant,
                Category = t.Category,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResponse<TransactionResponse>
        {
            Data = transactions,
            Total = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (totalCount + pageSize - 1) / pageSize,
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(int id, CancellationToken cancellationToken)
    {
        var transaction = await context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Transaction not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} does not exist",
            });
        }

        return Ok(new TransactionResponse
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt,
        });
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateTransactionRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", validationErrors),
            });
        }

        var cardExists = await context.CreditCards
            .AnyAsync(c => c.Id == request.CreditCardId, cancellationToken);

        if (!cardExists)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Credit card with ID {request.CreditCardId} does not exist",
            });
        }

        var transaction = new Transaction
        {
            CreditCardId = request.CreditCardId,
            Amount = request.Amount,
            Merchant = request.Merchant.Trim(),
            Category = request.Category?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);

        var response = new TransactionResponse
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt,
        };

        await producer.PublishTransactionCreatedAsync(response, cancellationToken);

        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(
        int id,
        [FromBody] UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Transaction not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} does not exist",
            });
        }

        if (request.Amount.HasValue)
        {
            if (request.Amount.Value <= 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "amount must be > 0",
                });
            }
            transaction.Amount = request.Amount.Value;
        }

        if (!string.IsNullOrEmpty(request.Merchant))
        {
            var trimmed = request.Merchant.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "merchant cannot be empty",
                });
            }
            transaction.Merchant = trimmed;
        }

        if (request.Category is not null)
        {
            transaction.Category = request.Category.Trim();
        }

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id, CancellationToken cancellationToken)
    {
        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Transaction not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} does not exist",
            });
        }

        context.Transactions.Remove(transaction);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static List<string> ValidateCreateTransactionRequest(CreateTransactionRequest request)
    {
        var errors = new List<string>();

        if (request.Amount <= 0)
        {
            errors.Add("amount must be > 0");
        }

        if (string.IsNullOrWhiteSpace(request.Merchant))
        {
            errors.Add("merchant is required and cannot be empty");
        }

        return errors;
    }
}
