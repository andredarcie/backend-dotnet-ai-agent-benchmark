using Asp.Versioning;
using CreditCardApi.Api.Observability;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Controllers;

/// <summary>Endpoints for managing transactions.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/transactions")]
[Produces("application/json")]
public sealed class TransactionsController : ControllerBase
{
    private readonly TransactionService _service;

    /// <summary>Creates the controller.</summary>
    public TransactionsController(TransactionService service) => _service = service;

    /// <summary>Returns a paginated list of transactions.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> List(
        [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await _service.ListAsync(PageRequest.From(page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns a single transaction by id.</summary>
    [HttpGet("{id:int}", Name = "GetTransactionById")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var transaction = await _service.GetAsync(id, cancellationToken);
        return Ok(transaction);
    }

    /// <summary>Creates a transaction and publishes a <c>transaction.created</c> event.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> Create(
        [FromBody] CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        DiagnosticsConfig.TransactionsCreated.Add(1);
        return CreatedAtRoute("GetTransactionById", new { id = created.Id }, created);
    }

    /// <summary>Updates an existing transaction.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Deletes a transaction.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
