using System;
using System.Threading.Tasks;
using Gemini.Models;
using Gemini.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Gemini.Controllers;

[ApiController]
[Route("api/credit-cards")]
public class CreditCardsController : ControllerBase
{
    private readonly GetAllCreditCardsUseCase _getAllCreditCardsUseCase;
    private readonly GetCreditCardByIdUseCase _getCreditCardByIdUseCase;
    private readonly CreateCreditCardUseCase _createCreditCardUseCase;
    private readonly UpdateCreditCardUseCase _updateCreditCardUseCase;
    private readonly DeleteCreditCardUseCase _deleteCreditCardUseCase;
    private readonly GetTransactionsByCreditCardIdUseCase _getTransactionsByCreditCardIdUseCase;

    public CreditCardsController(
        GetAllCreditCardsUseCase getAllCreditCardsUseCase,
        GetCreditCardByIdUseCase getCreditCardByIdUseCase,
        CreateCreditCardUseCase createCreditCardUseCase,
        UpdateCreditCardUseCase updateCreditCardUseCase,
        DeleteCreditCardUseCase deleteCreditCardUseCase,
        GetTransactionsByCreditCardIdUseCase getTransactionsByCreditCardIdUseCase)
    {
        _getAllCreditCardsUseCase = getAllCreditCardsUseCase;
        _getCreditCardByIdUseCase = getCreditCardByIdUseCase;
        _createCreditCardUseCase = createCreditCardUseCase;
        _updateCreditCardUseCase = updateCreditCardUseCase;
        _deleteCreditCardUseCase = deleteCreditCardUseCase;
        _getTransactionsByCreditCardIdUseCase = getTransactionsByCreditCardIdUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cards = await _getAllCreditCardsUseCase.ExecuteAsync();
        return Ok(cards);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var card = await _getCreditCardByIdUseCase.ExecuteAsync(id);
        if (card == null)
        {
            return NotFound();
        }
        return Ok(card);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreditCard creditCard)
    {
        try
        {
            var createdCard = await _createCreditCardUseCase.ExecuteAsync(creditCard);
            return CreatedAtAction(nameof(GetById), new { id = createdCard.Id }, createdCard);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreditCard creditCard)
    {
        try
        {
            var updated = await _updateCreditCardUseCase.ExecuteAsync(id, creditCard);
            if (updated == null)
            {
                return NotFound();
            }
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _deleteCreditCardUseCase.ExecuteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetTransactions(int id)
    {
        var transactions = await _getTransactionsByCreditCardIdUseCase.ExecuteAsync(id);
        if (transactions == null)
        {
            return NotFound();
        }
        return Ok(transactions);
    }
}
