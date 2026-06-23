using CreditCardApi.DTOs;
using CreditCardApi.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/credit-cards")]
public class CreditCardsController : ControllerBase
{
    private readonly GetAllCreditCardsUseCase _getAll;
    private readonly GetCreditCardByIdUseCase _getById;
    private readonly CreateCreditCardUseCase _create;
    private readonly UpdateCreditCardUseCase _update;
    private readonly DeleteCreditCardUseCase _delete;
    private readonly GetCreditCardTransactionsUseCase _getTransactions;

    public CreditCardsController(
        GetAllCreditCardsUseCase getAll,
        GetCreditCardByIdUseCase getById,
        CreateCreditCardUseCase create,
        UpdateCreditCardUseCase update,
        DeleteCreditCardUseCase delete,
        GetCreditCardTransactionsUseCase getTransactions)
    {
        _getAll = getAll;
        _getById = getById;
        _create = create;
        _update = update;
        _delete = delete;
        _getTransactions = getTransactions;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cards = await _getAll.ExecuteAsync();
        return Ok(cards.Select(c => new CreditCardResponse
        {
            Id = c.Id,
            CardholderName = c.CardholderName,
            CardNumber = c.CardNumber,
            Brand = c.Brand,
            CreditLimit = c.CreditLimit,
            CreatedAt = c.CreatedAt
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var card = await _getById.ExecuteAsync(id);
        if (card is null) return NotFound();
        return Ok(new CreditCardResponse
        {
            Id = card.Id,
            CardholderName = card.CardholderName,
            CardNumber = card.CardNumber,
            Brand = card.Brand,
            CreditLimit = card.CreditLimit,
            CreatedAt = card.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardRequest request)
    {
        var (card, error) = await _create.ExecuteAsync(request);
        if (error is not null) return BadRequest(new { error });
        var response = new CreditCardResponse
        {
            Id = card!.Id,
            CardholderName = card.CardholderName,
            CardNumber = card.CardNumber,
            Brand = card.Brand,
            CreditLimit = card.CreditLimit,
            CreatedAt = card.CreatedAt
        };
        return CreatedAtAction(nameof(GetById), new { id = card.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCreditCardRequest request)
    {
        var (card, error, notFound) = await _update.ExecuteAsync(id, request);
        if (notFound) return NotFound();
        if (error is not null) return BadRequest(new { error });
        return Ok(new CreditCardResponse
        {
            Id = card!.Id,
            CardholderName = card.CardholderName,
            CardNumber = card.CardNumber,
            Brand = card.Brand,
            CreditLimit = card.CreditLimit,
            CreatedAt = card.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _delete.ExecuteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetTransactions(int id)
    {
        var (transactions, notFound) = await _getTransactions.ExecuteAsync(id);
        if (notFound) return NotFound();
        return Ok(transactions!.Select(t => new TransactionResponse
        {
            Id = t.Id,
            CreditCardId = t.CreditCardId,
            Amount = t.Amount,
            Merchant = t.Merchant,
            Category = t.Category,
            CreatedAt = t.CreatedAt
        }));
    }
}
