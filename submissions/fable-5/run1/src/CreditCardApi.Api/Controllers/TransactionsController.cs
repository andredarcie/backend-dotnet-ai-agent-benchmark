using Asp.Versioning;
using CreditCardApi.Api.Http;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Controllers;

/// <summary>CRUD operations for transactions.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly TransactionService _transactions;

    /// <summary>Creates the controller.</summary>
    public TransactionsController(TransactionService transactions)
    {
        _transactions = transactions;
    }

    /// <summary>Lists transactions, paginated. Pagination metadata is returned in <c>X-*</c> headers.</summary>
    /// <param name="pagination">Page number and size.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="200">The requested page of transactions.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetAll(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var page = await _transactions.GetPageAsync(pagination, cancellationToken);
        Response.WritePaginationHeaders(page);
        return Ok(page.Items);
    }

    /// <summary>Returns a single transaction by id.</summary>
    /// <param name="id">Transaction id.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="200">The transaction.</response>
    /// <response code="404">No transaction with that id exists.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetAsync(id, cancellationToken);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    /// <summary>
    /// Creates a transaction. On success an event is published to the <c>transactions</c> Kafka
    /// topic (via the transactional outbox), keyed by the transaction id.
    /// </summary>
    /// <param name="request">The transaction to create.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="201">The created transaction, with its <c>id</c> and a <c>Location</c> header.</response>
    /// <response code="400">A field is invalid or the referenced credit card does not exist.</response>
    [HttpPost]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> Create(
        TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _transactions.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Replaces a transaction's data.</summary>
    /// <param name="id">Transaction id.</param>
    /// <param name="request">The new transaction data.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="200">The updated transaction.</response>
    /// <response code="400">A field is invalid or the referenced credit card does not exist.</response>
    /// <response code="404">No transaction with that id exists.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> Update(
        int id,
        TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _transactions.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Deletes a transaction.</summary>
    /// <param name="id">Transaction id.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="204">The transaction was deleted.</response>
    /// <response code="404">No transaction with that id exists.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _transactions.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
