using CreditCardApi.Api.Application.Dto;
using CreditCardApi.Api.Domain.Entities;
using CreditCardApi.Api.Infrastructure.Data;
using CreditCardApi.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Api.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CreditCardsController(CreditCardDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<CreditCardDto>>> GetCreditCards(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var cards = await dbContext.CreditCards
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(cards.Select(CreditCardDto.FromEntity));
    }

    [HttpGet("{id}")]
    [Produces("application/json", "application/problem+json")]
    public async Task<ActionResult<CreditCardDto>> GetCreditCard(int id, CancellationToken cancellationToken = default)
    {
        var card = await dbContext.CreditCards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (card == null)
            return NotFound(CreateProblemDetails(404, "Not Found", $"Credit card with ID {id} not found"));

        return Ok(CreditCardDto.FromEntity(card));
    }

    [HttpPost]
    [Produces("application/json", "application/problem+json")]
    public async Task<ActionResult<CreditCardDto>> CreateCreditCard(
        [FromBody] CreateCreditCardRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CardholderName))
            return BadRequest(CreateProblemDetails(400, "Bad Request", "cardholderName is required and cannot be empty"));

        if (string.IsNullOrWhiteSpace(request.CardNumber))
            return BadRequest(CreateProblemDetails(400, "Bad Request", "cardNumber is required and cannot be empty"));

        if (!CardNumberProtection.IsValidCardNumber(request.CardNumber))
            return BadRequest(CreateProblemDetails(400, "Bad Request", "cardNumber is invalid"));

        if (request.CreditLimit < 0)
            return BadRequest(CreateProblemDetails(400, "Bad Request", "creditLimit must be >= 0"));

        var card = new CreditCard
        {
            CardholderName = request.CardholderName,
            CardNumber = request.CardNumber,
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.CreditCards.Add(card);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCreditCard), new { id = card.Id }, CreditCardDto.FromEntity(card));
    }

    [HttpPut("{id}")]
    [Produces("application/json", "application/problem+json")]
    public async Task<IActionResult> UpdateCreditCard(
        int id,
        [FromBody] UpdateCreditCardRequest request,
        CancellationToken cancellationToken = default)
    {
        var card = await dbContext.CreditCards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (card == null)
            return NotFound(CreateProblemDetails(404, "Not Found", $"Credit card with ID {id} not found"));

        if (!string.IsNullOrWhiteSpace(request.CardholderName))
            card.CardholderName = request.CardholderName;

        if (!string.IsNullOrWhiteSpace(request.CardNumber))
        {
            if (!CardNumberProtection.IsValidCardNumber(request.CardNumber))
                return BadRequest(CreateProblemDetails(400, "Bad Request", "cardNumber is invalid"));
            card.CardNumber = request.CardNumber;
        }

        if (request.Brand != null)
            card.Brand = request.Brand;

        if (request.CreditLimit.HasValue)
        {
            if (request.CreditLimit < 0)
                return BadRequest(CreateProblemDetails(400, "Bad Request", "creditLimit must be >= 0"));
            card.CreditLimit = request.CreditLimit.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Produces("application/problem+json")]
    public async Task<IActionResult> DeleteCreditCard(int id, CancellationToken cancellationToken = default)
    {
        var card = await dbContext.CreditCards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (card == null)
            return NotFound(CreateProblemDetails(404, "Not Found", $"Credit card with ID {id} not found"));

        dbContext.CreditCards.Remove(card);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("{id}/transactions")]
    [Produces("application/json", "application/problem+json")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetCreditCardTransactions(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var cardExists = await dbContext.CreditCards.AnyAsync(c => c.Id == id, cancellationToken);
        if (!cardExists)
            return NotFound(CreateProblemDetails(404, "Not Found", $"Credit card with ID {id} not found"));

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.CreditCardId == id)
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

    private static ProblemDetails CreateProblemDetails(int status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpwg.org/specs/rfc9110.html#status.{status}",
            Instance = ""
        };
    }
}
