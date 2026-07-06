using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CreditCardApi.Application.Common.Interfaces;
using CreditCardApi.Application.DTOs;
using CreditCardApi.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.WebApi.Controllers;

[ApiController]
[Route("api/credit-cards")]
[Produces("application/json")]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardRepository _cardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreditCardsController(
        ICreditCardRepository cardRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Gets a paginated list of credit cards.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CreditCardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CreditCardDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Cap page size for performance

        var items = await _cardRepository.GetPagedAsync(page, pageSize, cancellationToken);
        var totalCount = await _cardRepository.GetTotalCountAsync(cancellationToken);

        var dtos = items.Select(CreditCardDto.FromEntity);

        return Ok(new PagedResponse<CreditCardDto>(dtos, page, pageSize, totalCount));
    }

    /// <summary>
    /// Gets a credit card by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CreditCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardDto>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var card = await _cardRepository.GetByIdAsync(id, cancellationToken);
        if (card == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} was not found."
            });
        }

        return Ok(CreditCardDto.FromEntity(card));
    }

    /// <summary>
    /// Creates a new credit card.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreditCardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditCardDto>> Create(
        [FromBody] CreateCreditCardDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.CardholderName) || string.IsNullOrWhiteSpace(dto.CardNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "CardholderName and CardNumber cannot be empty."
            });
        }

        var card = new CreditCard(dto.CardholderName, dto.CardNumber, dto.Brand, dto.CreditLimit);

        await _cardRepository.AddAsync(card, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var responseDto = CreditCardDto.FromEntity(card);

        return CreatedAtAction(nameof(GetById), new { id = card.Id }, responseDto);
    }

    /// <summary>
    /// Updates an existing credit card.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCreditCardDto dto,
        CancellationToken cancellationToken = default)
    {
        var card = await _cardRepository.GetByIdAsync(id, cancellationToken);
        if (card == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} was not found."
            });
        }

        // Apply changes (entity validation is executed in properties setter)
        try
        {
            card.CardholderName = dto.CardholderName;
            card.CardNumber = dto.CardNumber;
            card.Brand = dto.Brand;
            card.CreditLimit = dto.CreditLimit;
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Deletes a credit card.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        var card = await _cardRepository.GetByIdAsync(id, cancellationToken);
        if (card == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} was not found."
            });
        }

        await _cardRepository.DeleteAsync(card, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Gets all transactions associated with a credit card.
    /// </summary>
    [HttpGet("{id:int}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        int id,
        CancellationToken cancellationToken = default)
    {
        var cardExists = await _cardRepository.ExistsAsync(id, cancellationToken);
        if (!cardExists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Credit card with ID {id} was not found."
            });
        }

        var txns = await _transactionRepository.GetByCreditCardIdAsync(id, cancellationToken);
        var dtos = txns.Select(TransactionDto.FromEntity);

        return Ok(dtos);
    }
}
