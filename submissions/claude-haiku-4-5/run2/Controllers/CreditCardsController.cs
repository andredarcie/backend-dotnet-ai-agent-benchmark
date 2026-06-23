using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/credit-cards")]
public class CreditCardsController(
    GetAllCreditCardsUseCase getAllCreditCardsUseCase,
    GetCreditCardByIdUseCase getCreditCardByIdUseCase,
    CreateCreditCardUseCase createCreditCardUseCase,
    UpdateCreditCardUseCase updateCreditCardUseCase,
    DeleteCreditCardUseCase deleteCreditCardUseCase,
    GetCreditCardTransactionsUseCase getCreditCardTransactionsUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var creditCards = await getAllCreditCardsUseCase.ExecuteAsync();
        return Ok(creditCards);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var creditCard = await getCreditCardByIdUseCase.ExecuteAsync(id);
        if (creditCard == null)
            return NotFound();

        return Ok(creditCard);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardDto dto)
    {
        try
        {
            var creditCard = await createCreditCardUseCase.ExecuteAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = creditCard.Id }, creditCard);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCreditCardDto dto)
    {
        try
        {
            var creditCard = await updateCreditCardUseCase.ExecuteAsync(id, dto);
            if (creditCard == null)
                return NotFound();

            return Ok(creditCard);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await deleteCreditCardUseCase.ExecuteAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetTransactions(int id)
    {
        var transactions = await getCreditCardTransactionsUseCase.ExecuteAsync(id);
        if (transactions == null)
            return NotFound();

        return Ok(transactions);
    }
}
