using Asp.Versioning;
using CreditCardApi.Api.Http;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Controllers;

/// <summary>CRUD operations for credit cards.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/credit-cards")]
public sealed class CreditCardsController : ControllerBase
{
    private readonly CreditCardService _creditCards;

    /// <summary>Creates the controller.</summary>
    public CreditCardsController(CreditCardService creditCards)
    {
        _creditCards = creditCards;
    }

    /// <summary>Lists credit cards, paginated. Pagination metadata is returned in <c>X-*</c> headers.</summary>
    /// <param name="pagination">Page number and size.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="200">The requested page of credit cards.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CreditCardResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> GetAll(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var page = await _creditCards.GetPageAsync(pagination, cancellationToken);
        Response.WritePaginationHeaders(page);
        return Ok(page.Items);
    }

    /// <summary>Returns a single credit card by id.</summary>
    /// <param name="id">Credit card id.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="200">The credit card.</response>
    /// <response code="404">No credit card with that id exists.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var card = await _creditCards.GetAsync(id, cancellationToken);
        return card is null ? NotFound() : Ok(card);
    }

    /// <summary>Creates a credit card. The card number is stored truncated and returned masked.</summary>
    /// <param name="request">The card to create.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="201">The created card, with its <c>id</c> and a <c>Location</c> header.</response>
    /// <response code="400">A required field is missing or invalid.</response>
    [HttpPost]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditCardResponse>> Create(
        CreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _creditCards.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Replaces a credit card's data.</summary>
    /// <param name="id">Credit card id.</param>
    /// <param name="request">The new card data.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="200">The updated card.</response>
    /// <response code="400">A required field is missing or invalid.</response>
    /// <response code="404">No credit card with that id exists.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardResponse>> Update(
        int id,
        CreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _creditCards.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Deletes a credit card and all of its transactions.</summary>
    /// <param name="id">Credit card id.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="204">The card was deleted.</response>
    /// <response code="404">No credit card with that id exists.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _creditCards.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Lists the transactions of a credit card, paginated.</summary>
    /// <param name="id">Credit card id.</param>
    /// <param name="pagination">Page number and size.</param>
    /// <param name="cancellationToken">Aborts the operation if the client disconnects.</param>
    /// <response code="200">The requested page of the card's transactions.</response>
    /// <response code="404">No credit card with that id exists.</response>
    [HttpGet("{id:int}/transactions")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetTransactions(
        int id,
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var page = await _creditCards.GetTransactionsAsync(id, pagination, cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        Response.WritePaginationHeaders(page);
        return Ok(page.Items);
    }
}
