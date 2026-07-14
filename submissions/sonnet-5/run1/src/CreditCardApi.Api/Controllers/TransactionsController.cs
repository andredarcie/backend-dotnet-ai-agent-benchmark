using CreditCardApi.Application.Transactions;
using CreditCardApi.Application.Transactions.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    /// <summary>Lists transactions, newest-id-last, page by page.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var transactions = await transactionService.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        return Ok(transactions);
    }

    /// <summary>Gets a single transaction by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var transaction = await transactionService.GetByIdAsync(id, cancellationToken);
        return Ok(transaction);
    }

    /// <summary>
    /// Creates a transaction against an existing card. On success, publishes the transaction to
    /// the Kafka "transactions" topic after the row is persisted.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> Create(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = await transactionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
    }
}
