using Asp.Versioning;
using CreditCardApi.Api.Http;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/credit-cards")]
public class CreditCardsController(CreditCardService creditCardService) : ControllerBase
{
    /// <summary>Lists credit cards, paginated.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CreditCardResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> List(
        [FromQuery] int page = PaginationQuery.DefaultPage,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await creditCardService.ListAsync(new PaginationQuery { Page = page, PageSize = pageSize }, cancellationToken);
        Response.AddPaginationHeaders(result);
        return Ok(result.Items);
    }

    /// <summary>Gets a single credit card by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await creditCardService.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates a credit card. The submitted card number is truncated to its last 4 digits before storage.</summary>
    [HttpPost]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditCardResponse>> Create(CreditCardRequest request, CancellationToken cancellationToken)
    {
        var created = await creditCardService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Replaces a credit card's details.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardResponse>> Update(int id, CreditCardRequest request, CancellationToken cancellationToken) =>
        Ok(await creditCardService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes a credit card and cascades to its transactions.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await creditCardService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Lists the transactions billed to a credit card, paginated.</summary>
    [HttpGet("{id:int}/transactions")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetTransactions(
        int id,
        [FromQuery] int page = PaginationQuery.DefaultPage,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await creditCardService.ListTransactionsAsync(
            id, new PaginationQuery { Page = page, PageSize = pageSize }, cancellationToken);
        Response.AddPaginationHeaders(result);
        return Ok(result.Items);
    }
}
