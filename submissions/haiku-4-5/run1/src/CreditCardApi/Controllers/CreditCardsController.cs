using CreditCardApi.Application.DTOs;
using CreditCardApi.Application.Repositories;
using CreditCardApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

/// <summary>
/// Credit cards API endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CreditCardsController(ICreditCardRepository repository, ITransactionRepository transactionRepository) : ControllerBase
{
    /// <summary>
    /// Get all credit cards (paginated).
    /// </summary>
    /// <param name="pageNumber">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of credit cards.</returns>
    [HttpGet]
    [ProduceResponseType(typeof(IEnumerable<CreditCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var cards = await repository.GetAllAsync(pageNumber, pageSize, cancellationToken);
        var dtos = cards.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Get a credit card by ID.
    /// </summary>
    /// <param name="id">The credit card ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The credit card.</returns>
    [HttpGet("{id}")]
    [ProduceResponseType(typeof(CreditCardDto), StatusCodes.Status200OK)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var card = await repository.GetByIdAsync(id, cancellationToken);
        if (card == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} not found."
            });
        }

        return Ok(MapToDto(card));
    }

    /// <summary>
    /// Create a new credit card.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created credit card.</returns>
    [HttpPost]
    [ProduceResponseType(typeof(CreditCardDto), StatusCodes.Status201Created)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CardholderName))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Cardholder name is required and cannot be empty."
            });
        }

        if (string.IsNullOrWhiteSpace(request.CardNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Card number is required and cannot be empty."
            });
        }

        if (request.CreditLimit < 0)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Credit limit must be greater than or equal to 0."
            });
        }

        var card = new CreditCard
        {
            CardholderName = request.CardholderName,
            CardNumber = request.CardNumber,
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        var createdCard = await repository.CreateAsync(card, cancellationToken);
        var dto = MapToDto(createdCard);

        return CreatedAtAction(nameof(GetById), new { id = createdCard.Id }, dto);
    }

    /// <summary>
    /// Update a credit card.
    /// </summary>
    /// <param name="id">The credit card ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated credit card.</returns>
    [HttpPut("{id}")]
    [ProduceResponseType(typeof(CreditCardDto), StatusCodes.Status200OK)]
    [ProduceResponseType(StatusCodes.Status204NoContent)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCreditCardRequest request, CancellationToken cancellationToken = default)
    {
        var existingCard = await repository.GetByIdAsync(id, cancellationToken);
        if (existingCard == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} not found."
            });
        }

        if (request.CardholderName != null && string.IsNullOrWhiteSpace(request.CardholderName))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Cardholder name cannot be empty."
            });
        }

        if (request.CardNumber != null && string.IsNullOrWhiteSpace(request.CardNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Card number cannot be empty."
            });
        }

        if (request.CreditLimit.HasValue && request.CreditLimit < 0)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://example.com/errors/validation",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Credit limit must be greater than or equal to 0."
            });
        }

        var cardToUpdate = new CreditCard
        {
            CardholderName = request.CardholderName ?? existingCard.CardholderName,
            CardNumber = request.CardNumber ?? existingCard.CardNumber,
            Brand = request.Brand ?? existingCard.Brand,
            CreditLimit = request.CreditLimit ?? existingCard.CreditLimit
        };

        var updatedCard = await repository.UpdateAsync(id, cardToUpdate, cancellationToken);
        if (updatedCard == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(updatedCard));
    }

    /// <summary>
    /// Delete a credit card.
    /// </summary>
    /// <param name="id">The credit card ID.</param>
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
                Detail = $"Credit card with ID {id} not found."
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Get transactions for a credit card.
    /// </summary>
    /// <param name="id">The credit card ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of transactions for the card.</returns>
    [HttpGet("{id}/transactions")]
    [ProduceResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    [ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactions(int id, CancellationToken cancellationToken = default)
    {
        var card = await repository.GetByIdAsync(id, cancellationToken);
        if (card == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} not found."
            });
        }

        var transactions = await transactionRepository.GetByCreditCardIdAsync(id, cancellationToken);
        var dtos = transactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            CreditCardId = t.CreditCardId,
            Amount = t.Amount,
            Merchant = t.Merchant,
            Category = t.Category,
            CreatedAt = t.CreatedAt
        });

        return Ok(dtos);
    }

    private static CreditCardDto MapToDto(CreditCard card)
    {
        return new CreditCardDto
        {
            Id = card.Id,
            CardholderName = card.CardholderName,
            CardNumber = MaskCardNumber(card.CardNumber),
            Brand = card.Brand,
            CreditLimit = card.CreditLimit,
            CreatedAt = card.CreatedAt
        };
    }

    private static string MaskCardNumber(string cardNumber)
    {
        if (cardNumber.Length <= 4)
        {
            return "****";
        }

        return $"****{cardNumber[^4..]}";
    }
}
