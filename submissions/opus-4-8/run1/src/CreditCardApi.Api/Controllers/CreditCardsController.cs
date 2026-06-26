using Asp.Versioning;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Controllers;

/// <summary>Endpoints for managing credit cards.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/credit-cards")]
[Produces("application/json")]
public sealed class CreditCardsController : ControllerBase
{
    private readonly CreditCardService _service;

    /// <summary>Creates the controller.</summary>
    public CreditCardsController(CreditCardService service) => _service = service;

    /// <summary>Returns a paginated list of credit cards.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CreditCardResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CreditCardResponse>>> List(
        [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await _service.ListAsync(PageRequest.From(page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns a single credit card by id.</summary>
    [HttpGet("{id:int}", Name = "GetCreditCardById")]
    [ProducesResponseType(typeof(CreditCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var card = await _service.GetAsync(id, cancellationToken);
        return Ok(card);
    }

    /// <summary>Creates a new credit card.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreditCardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditCardResponse>> Create(
        [FromBody] CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetCreditCardById", new { id = created.Id }, created);
    }

    /// <summary>Updates an existing credit card.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateCreditCardRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Deletes a credit card and its transactions.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns a paginated list of the transactions for a card.</summary>
    [HttpGet("{id:int}/transactions")]
    [ProducesResponseType(typeof(PagedResult<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> GetTransactions(
        int id, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await _service.ListTransactionsAsync(id, PageRequest.From(page, pageSize), cancellationToken);
        return Ok(result);
    }
}
