using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.CreditCards.Dtos;
using CreditCardApi.Application.Transactions.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Controllers;

[ApiController]
[Route("api/credit-cards")]
public sealed class CreditCardsController(ICreditCardService creditCardService) : ControllerBase
{
    /// <summary>Lists credit cards, newest-id-last, page by page.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CreditCardResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var creditCards = await creditCardService.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        return Ok(creditCards);
    }

    /// <summary>Gets a single credit card by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var creditCard = await creditCardService.GetByIdAsync(id, cancellationToken);
        return Ok(creditCard);
    }

    /// <summary>Creates a credit card. The PAN is encrypted at rest and only ever returned masked.</summary>
    [HttpPost]
    [ProducesResponseType<CreditCardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditCardResponse>> Create(CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var creditCard = await creditCardService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = creditCard.Id }, creditCard);
    }

    /// <summary>Lists every transaction posted against this card.</summary>
    [HttpGet("{id:int}/transactions")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetTransactions(int id, CancellationToken cancellationToken)
    {
        var transactions = await creditCardService.GetTransactionsForCardAsync(id, cancellationToken);
        return Ok(transactions);
    }
}
