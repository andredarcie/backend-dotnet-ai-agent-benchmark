using CreditCardApi.Api.Application.Dto;
using CreditCardApi.Api.Domain.Entities;
using CreditCardApi.Api.Infrastructure.Data;
using CreditCardApi.Api.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Api.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TransactionsController(
    CreditCardDbContext dbContext,
    ITransactionProducer transactionProducer) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                CreditCardId = t.CreditCardId,
                Amount = t.Amount,
                Merchant = t.Merchant,
                Category = t.Category,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(transactions);
    }

    [HttpGet("{id}")]
    [Produces("application/json", "application/problem+json")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction == null)
            return NotFound(CreateProblemDetails(404, "Not Found", $"Transaction with ID {id} not found"));

        return Ok(new TransactionDto
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt
        });
    }

    [HttpPost]
    [Produces("application/json", "application/problem+json")]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Merchant))
            return BadRequest(CreateProblemDetails(400, "Bad Request", "merchant is required and cannot be empty"));

        if (request.Amount <= 0)
            return BadRequest(CreateProblemDetails(400, "Bad Request", "amount must be greater than 0"));

        var cardExists = await dbContext.CreditCards.AnyAsync(c => c.Id == request.CreditCardId, cancellationToken);
        if (!cardExists)
            return BadRequest(CreateProblemDetails(400, "Bad Request", $"Credit card with ID {request.CreditCardId} does not exist"));

        var transaction = new Transaction
        {
            CreditCardId = request.CreditCardId,
            Amount = request.Amount,
            Merchant = request.Merchant,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new TransactionDto
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt
        };

        await transactionProducer.PublishTransactionAsync(dto, cancellationToken);

        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, dto);
    }

    [HttpPut("{id}")]
    [Produces("application/json", "application/problem+json")]
    public async Task<IActionResult> UpdateTransaction(
        int id,
        [FromBody] UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction == null)
            return NotFound(CreateProblemDetails(404, "Not Found", $"Transaction with ID {id} not found"));

        if (request.Amount.HasValue && request.Amount <= 0)
            return BadRequest(CreateProblemDetails(400, "Bad Request", "amount must be greater than 0"));

        if (request.CreditCardId.HasValue)
        {
            var cardExists = await dbContext.CreditCards.AnyAsync(c => c.Id == request.CreditCardId, cancellationToken);
            if (!cardExists)
                return BadRequest(CreateProblemDetails(400, "Bad Request", $"Credit card with ID {request.CreditCardId} does not exist"));
            transaction.CreditCardId = request.CreditCardId.Value;
        }

        if (request.Amount.HasValue)
            transaction.Amount = request.Amount.Value;

        if (!string.IsNullOrWhiteSpace(request.Merchant))
            transaction.Merchant = request.Merchant;

        if (request.Category != null)
            transaction.Category = request.Category;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Produces("application/problem+json")]
    public async Task<IActionResult> DeleteTransaction(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction == null)
            return NotFound(CreateProblemDetails(404, "Not Found", $"Transaction with ID {id} not found"));

        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static ProblemDetails CreateProblemDetails(int status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpwg.org/specs/rfc9110.html#status.{status}"
        };
    }
}
