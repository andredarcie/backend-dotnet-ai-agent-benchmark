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
[Route("api/transactions")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICreditCardRepository _cardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionsController(
        ITransactionRepository transactionRepository,
        ICreditCardRepository cardRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _cardRepository = cardRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Gets a paginated list of transactions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TransactionDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var items = await _transactionRepository.GetPagedAsync(page, pageSize, cancellationToken);
        var totalCount = await _transactionRepository.GetTotalCountAsync(cancellationToken);

        var dtos = items.Select(TransactionDto.FromEntity);

        return Ok(new PagedResponse<TransactionDto>(dtos, page, pageSize, totalCount));
    }

    /// <summary>
    /// Gets a transaction by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var txn = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (txn == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} was not found."
            });
        }

        return Ok(TransactionDto.FromEntity(txn));
    }

    /// <summary>
    /// Creates a new transaction against a credit card.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionDto>> Create(
        [FromBody] CreateTransactionDto dto,
        CancellationToken cancellationToken = default)
    {
        // 1. Basic field validation
        if (string.IsNullOrWhiteSpace(dto.Merchant))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Merchant name is required."
            });
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Transaction amount must be greater than 0."
            });
        }

        // 2. Validate that referenced creditCardId exists
        var cardExists = await _cardRepository.ExistsAsync(dto.CreditCardId, cancellationToken);
        if (!cardExists)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Referenced Credit Card with ID {dto.CreditCardId} does not exist."
            });
        }

        var txn = new Transaction(dto.CreditCardId, dto.Amount, dto.Merchant, dto.Category);

        await _transactionRepository.AddAsync(txn, cancellationToken);
        
        // This will write the Transaction AND the OutboxMessage atomically in a single DB transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var responseDto = TransactionDto.FromEntity(txn);

        return CreatedAtAction(nameof(GetById), new { id = txn.Id }, responseDto);
    }

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTransactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var txn = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (txn == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} was not found."
            });
        }

        // Validate referencing card exists if it's changing
        if (txn.CreditCardId != dto.CreditCardId)
        {
            var cardExists = await _cardRepository.ExistsAsync(dto.CreditCardId, cancellationToken);
            if (!cardExists)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Referenced Credit Card with ID {dto.CreditCardId} does not exist."
                });
            }
        }

        try
        {
            txn.CreditCardId = dto.CreditCardId;
            txn.Amount = dto.Amount;
            txn.Merchant = dto.Merchant;
            txn.Category = dto.Category;
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
    /// Deletes a transaction.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        var txn = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (txn == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} was not found."
            });
        }

        await _transactionRepository.DeleteAsync(txn, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
