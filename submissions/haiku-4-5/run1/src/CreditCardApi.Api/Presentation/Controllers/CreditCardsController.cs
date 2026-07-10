using CreditCardApi.Api.Application.Dto;
using CreditCardApi.Api.Domain.Entities;
using CreditCardApi.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Api.Presentation.Controllers;

[ApiController]
[Route("api/credit-cards")]
[Produces("application/json", "application/problem+json")]
public class CreditCardsController(CreditCardDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CreditCardResponse>>> GetCreditCards(
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

        var totalCount = await context.CreditCards.CountAsync(cancellationToken);
        var cards = await context.CreditCards
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(c => new CreditCardResponse
            {
                Id = c.Id,
                CardholderName = c.CardholderName,
                CardNumber = c.CardNumber,
                Brand = c.Brand,
                CreditLimit = c.CreditLimit,
                CreatedAt = c.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResponse<CreditCardResponse>
        {
            Data = cards,
            Total = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (totalCount + pageSize - 1) / pageSize,
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CreditCardResponse>> GetCreditCard(int id, CancellationToken cancellationToken)
    {
        var card = await context.CreditCards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (card is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Credit card not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} does not exist",
            });
        }

        return Ok(new CreditCardResponse
        {
            Id = card.Id,
            CardholderName = card.CardholderName,
            CardNumber = card.CardNumber,
            Brand = card.Brand,
            CreditLimit = card.CreditLimit,
            CreatedAt = card.CreatedAt,
        });
    }

    [HttpPost]
    public async Task<ActionResult<CreditCardResponse>> CreateCreditCard(
        [FromBody] CreateCreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateCreditCardRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", validationErrors),
            });
        }

        var card = new CreditCard
        {
            CardholderName = request.CardholderName.Trim(),
            CardNumber = request.CardNumber.Trim(),
            Brand = request.Brand?.Trim(),
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow,
        };

        context.CreditCards.Add(card);
        await context.SaveChangesAsync(cancellationToken);

        var response = new CreditCardResponse
        {
            Id = card.Id,
            CardholderName = card.CardholderName,
            CardNumber = card.CardNumber,
            Brand = card.Brand,
            CreditLimit = card.CreditLimit,
            CreatedAt = card.CreatedAt,
        };

        return CreatedAtAction(nameof(GetCreditCard), new { id = card.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCreditCard(
        int id,
        [FromBody] UpdateCreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var card = await context.CreditCards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (card is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Credit card not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} does not exist",
            });
        }

        if (!string.IsNullOrEmpty(request.CardholderName))
        {
            var trimmed = request.CardholderName.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "cardholderName cannot be empty",
                });
            }
            card.CardholderName = trimmed;
        }

        if (!string.IsNullOrEmpty(request.CardNumber))
        {
            var trimmed = request.CardNumber.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "cardNumber cannot be empty",
                });
            }
            card.CardNumber = trimmed;
        }

        if (request.Brand is not null)
        {
            card.Brand = request.Brand.Trim();
        }

        if (request.CreditLimit.HasValue)
        {
            if (request.CreditLimit.Value < 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "creditLimit must be >= 0",
                });
            }
            card.CreditLimit = request.CreditLimit.Value;
        }

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCreditCard(int id, CancellationToken cancellationToken)
    {
        var card = await context.CreditCards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (card is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Credit card not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} does not exist",
            });
        }

        context.CreditCards.Remove(card);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<List<TransactionResponse>>> GetCreditCardTransactions(
        int id,
        CancellationToken cancellationToken)
    {
        var cardExists = await context.CreditCards.AnyAsync(c => c.Id == id, cancellationToken);
        if (!cardExists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Credit card not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} does not exist",
            });
        }

        var transactions = await context.Transactions
            .Where(t => t.CreditCardId == id)
            .OrderByDescending(t => t.CreatedAt)
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

        return Ok(transactions);
    }

    private static List<string> ValidateCreateCreditCardRequest(CreateCreditCardRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.CardholderName))
        {
            errors.Add("cardholderName is required and cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(request.CardNumber))
        {
            errors.Add("cardNumber is required and cannot be empty");
        }

        if (request.CreditLimit < 0)
        {
            errors.Add("creditLimit must be >= 0");
        }

        return errors;
    }
}

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
