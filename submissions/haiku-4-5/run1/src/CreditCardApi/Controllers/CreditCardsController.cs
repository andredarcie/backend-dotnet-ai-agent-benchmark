using CreditCardApi.Application.DTOs;
using CreditCardApi.Application.Repositories;
using CreditCardApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/credit-cards")]
[Produces("application/json")]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<CreditCardsController> _logger;

    public CreditCardsController(
        ICreditCardRepository creditCardRepository,
        ITransactionRepository transactionRepository,
        ILogger<CreditCardsController> logger)
    {
        _creditCardRepository = creditCardRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all credit cards with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://api.example.com/errors/invalid-pagination",
                Title = "Invalid pagination parameters",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Page number and page size must be greater than 0"
            });
        }

        var cards = await _creditCardRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);
        var dtos = cards.Select(c => new CreditCardDto(
            c.Id, c.CardholderName, MaskCardNumber(c.CardNumber), c.Brand, c.CreditLimit, c.CreatedAt));

        return Ok(dtos);
    }

    /// <summary>
    /// Get a specific credit card by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var card = await _creditCardRepository.GetByIdAsync(id, cancellationToken);
        if (card == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://api.example.com/errors/not-found",
                Title = "Credit card not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} not found"
            });
        }

        var dto = new CreditCardDto(
            card.Id, card.CardholderName, MaskCardNumber(card.CardNumber), card.Brand, card.CreditLimit, card.CreatedAt);
        return Ok(dto);
    }

    /// <summary>
    /// Create a new credit card
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateCreditCardRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CardholderName))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://api.example.com/errors/validation-error",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Cardholder name is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.CardNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://api.example.com/errors/validation-error",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Card number is required"
            });
        }

        if (request.CreditLimit < 0)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://api.example.com/errors/validation-error",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Credit limit must be greater than or equal to 0"
            });
        }

        var creditCard = new CreditCard
        {
            CardholderName = request.CardholderName,
            CardNumber = request.CardNumber,
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _creditCardRepository.AddAsync(creditCard, cancellationToken);
        var dto = new CreditCardDto(
            created.Id, created.CardholderName, MaskCardNumber(created.CardNumber), created.Brand, created.CreditLimit, created.CreatedAt);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);
    }

    /// <summary>
    /// Update an existing credit card
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCreditCardRequest request, CancellationToken cancellationToken = default)
    {
        var card = await _creditCardRepository.GetByIdAsync(id, cancellationToken);
        if (card == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://api.example.com/errors/not-found",
                Title = "Credit card not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} not found"
            });
        }

        if (!string.IsNullOrWhiteSpace(request.CardholderName))
        {
            card.CardholderName = request.CardholderName;
        }

        if (!string.IsNullOrWhiteSpace(request.CardNumber))
        {
            card.CardNumber = request.CardNumber;
        }

        if (request.Brand != null)
        {
            card.Brand = request.Brand;
        }

        if (request.CreditLimit.HasValue)
        {
            if (request.CreditLimit < 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Type = "https://api.example.com/errors/validation-error",
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Credit limit must be greater than or equal to 0"
                });
            }

            card.CreditLimit = request.CreditLimit.Value;
        }

        await _creditCardRepository.UpdateAsync(card, cancellationToken);
        var dto = new CreditCardDto(
            card.Id, card.CardholderName, MaskCardNumber(card.CardNumber), card.Brand, card.CreditLimit, card.CreatedAt);

        return Ok(dto);
    }

    /// <summary>
    /// Delete a credit card
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var card = await _creditCardRepository.GetByIdAsync(id, cancellationToken);
        if (card == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://api.example.com/errors/not-found",
                Title = "Credit card not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} not found"
            });
        }

        await _creditCardRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get all transactions for a specific credit card
    /// </summary>
    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetTransactions(int id, CancellationToken cancellationToken = default)
    {
        var card = await _creditCardRepository.GetByIdAsync(id, cancellationToken);
        if (card == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://api.example.com/errors/not-found",
                Title = "Credit card not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} not found"
            });
        }

        var transactions = await _transactionRepository.GetByCreditCardIdAsync(id, cancellationToken);
        var dtos = transactions.Select(t => new TransactionDto(
            t.Id, t.CreditCardId, t.Amount, t.Merchant, t.Category, t.CreatedAt));

        return Ok(dtos);
    }

    private static string MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
        {
            return "****";
        }

        return $"****{cardNumber.Substring(cardNumber.Length - 4)}";
    }
}
