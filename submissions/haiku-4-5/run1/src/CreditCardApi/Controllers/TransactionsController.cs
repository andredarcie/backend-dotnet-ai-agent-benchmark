using CreditCardApi.Application.DTOs;
using CreditCardApi.Application.Repositories;
using CreditCardApi.Application.Services;
using CreditCardApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/transactions")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionRepository transactionRepository,
        ITransactionService transactionService,
        ILogger<TransactionsController> logger)
    {
        _transactionRepository = transactionRepository;
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all transactions with pagination
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

        var transactions = await _transactionRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);
        var dtos = transactions.Select(t => new TransactionDto(
            t.Id, t.CreditCardId, t.Amount, t.Merchant, t.Category, t.CreatedAt));

        return Ok(dtos);
    }

    /// <summary>
    /// Get a specific transaction by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://api.example.com/errors/not-found",
                Title = "Transaction not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} not found"
            });
        }

        var dto = new TransactionDto(
            transaction.Id, transaction.CreditCardId, transaction.Amount, transaction.Merchant, transaction.Category, transaction.CreatedAt);
        return Ok(dto);
    }

    /// <summary>
    /// Create a new transaction
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Merchant))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://api.example.com/errors/validation-error",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Merchant is required"
            });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://api.example.com/errors/validation-error",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Amount must be greater than 0"
            });
        }

        try
        {
            var dto = await _transactionService.CreateTransactionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://api.example.com/errors/validation-error",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Update an existing transaction
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://api.example.com/errors/not-found",
                Title = "Transaction not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} not found"
            });
        }

        if (request.Amount.HasValue)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Type = "https://api.example.com/errors/validation-error",
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Amount must be greater than 0"
                });
            }

            transaction.Amount = request.Amount.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Merchant))
        {
            transaction.Merchant = request.Merchant;
        }

        if (request.Category != null)
        {
            transaction.Category = request.Category;
        }

        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        var dto = new TransactionDto(
            transaction.Id, transaction.CreditCardId, transaction.Amount, transaction.Merchant, transaction.Category, transaction.CreatedAt);

        return Ok(dto);
    }

    /// <summary>
    /// Delete a transaction
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://api.example.com/errors/not-found",
                Title = "Transaction not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Transaction with ID {id} not found"
            });
        }

        await _transactionRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
