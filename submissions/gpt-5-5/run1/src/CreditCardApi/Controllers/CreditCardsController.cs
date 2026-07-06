using Asp.Versioning;
using CreditCardApi.Application.Dtos;
using CreditCardApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

/// <summary>Credit card endpoints.</summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/credit-cards")]
[Produces("application/json")]
public sealed class CreditCardsController : ControllerBase
{
    private readonly CreditCardService _creditCardService;

    public CreditCardsController(CreditCardService creditCardService)
    {
        _creditCardService = creditCardService;
    }

    /// <summary>Lists credit cards using page and pageSize query parameters.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CreditCardResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> Get([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var page = await _creditCardService.GetPageAsync(query, cancellationToken);
        AddPaginationHeaders(page);
        return Ok(page.Items);
    }

    /// <summary>Gets a credit card by id.</summary>
    [HttpGet("{id:int}", Name = nameof(GetCreditCardById))]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardResponse>> GetCreditCardById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _creditCardService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Creates a credit card.</summary>
    [HttpPost]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditCardResponse>> Create(CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var response = await _creditCardService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetCreditCardById), new { id = response.Id }, response);
    }

    /// <summary>Replaces a credit card.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardResponse>> Update(int id, UpdateCreditCardRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _creditCardService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Deletes a credit card and its transactions.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _creditCardService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Lists transactions for a credit card.</summary>
    [HttpGet("{id:int}/transactions")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetTransactions(int id, CancellationToken cancellationToken)
    {
        return Ok(await _creditCardService.GetTransactionsAsync(id, cancellationToken));
    }

    private void AddPaginationHeaders<T>(PagedResult<T> page)
    {
        Response.Headers["X-Page"] = page.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Page-Size"] = page.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Total-Count"] = page.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
