using Asp.Versioning;
using CreditCardApi.Application.Dtos;
using CreditCardApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

/// <summary>Transaction endpoints.</summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/transactions")]
[Produces("application/json")]
public sealed class TransactionsController : ControllerBase
{
    private readonly TransactionService _transactionService;

    public TransactionsController(TransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>Lists transactions using page and pageSize query parameters.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> Get([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var page = await _transactionService.GetPageAsync(query, cancellationToken);
        AddPaginationHeaders(page);
        return Ok(page.Items);
    }

    /// <summary>Gets a transaction by id.</summary>
    [HttpGet("{id:int}", Name = nameof(GetTransactionById))]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetTransactionById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _transactionService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Creates a transaction and schedules its Kafka event through the outbox.</summary>
    [HttpPost]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> Create(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var response = await _transactionService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetTransactionById), new { id = response.Id }, response);
    }

    /// <summary>Replaces a transaction.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> Update(int id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _transactionService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Deletes a transaction.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _transactionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private void AddPaginationHeaders<T>(PagedResult<T> page)
    {
        Response.Headers["X-Page"] = page.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Page-Size"] = page.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Total-Count"] = page.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
