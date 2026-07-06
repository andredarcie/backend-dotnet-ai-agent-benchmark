using Asp.Versioning;
using CreditCardApi.Api.Http;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/transactions")]
public class TransactionsController(TransactionService transactionService) : ControllerBase
{
    /// <summary>Lists transactions across all cards, paginated.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> List(
        [FromQuery] int page = PaginationQuery.DefaultPage,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await transactionService.ListAsync(new PaginationQuery { Page = page, PageSize = pageSize }, cancellationToken);
        Response.AddPaginationHeaders(result);
        return Ok(result.Items);
    }

    /// <summary>Gets a single transaction by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await transactionService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Charges a credit card. On success, the created transaction is published to the <c>transactions</c>
    /// Kafka topic via a transactional outbox — keyed by the transaction id.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> Create(TransactionRequest request, CancellationToken cancellationToken)
    {
        var created = await transactionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a transaction's amount, merchant, or category. It cannot be moved to a different card.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> Update(int id, TransactionRequest request, CancellationToken cancellationToken) =>
        Ok(await transactionService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes a transaction.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await transactionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
