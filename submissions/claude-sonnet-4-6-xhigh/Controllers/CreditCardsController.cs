using CreditCardApi.DTOs;
using CreditCardApi.Exceptions;
using CreditCardApi.UseCases.CreditCards;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/credit-cards")]
public class CreditCardsController : ControllerBase
{
    private readonly GetAllCreditCardsUseCase _getAllUseCase;
    private readonly GetCreditCardByIdUseCase _getByIdUseCase;
    private readonly CreateCreditCardUseCase _createUseCase;
    private readonly UpdateCreditCardUseCase _updateUseCase;
    private readonly DeleteCreditCardUseCase _deleteUseCase;
    private readonly GetCreditCardTransactionsUseCase _getTransactionsUseCase;

    public CreditCardsController(
        GetAllCreditCardsUseCase getAllUseCase,
        GetCreditCardByIdUseCase getByIdUseCase,
        CreateCreditCardUseCase createUseCase,
        UpdateCreditCardUseCase updateUseCase,
        DeleteCreditCardUseCase deleteUseCase,
        GetCreditCardTransactionsUseCase getTransactionsUseCase)
    {
        _getAllUseCase = getAllUseCase;
        _getByIdUseCase = getByIdUseCase;
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _deleteUseCase = deleteUseCase;
        _getTransactionsUseCase = getTransactionsUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cards = await _getAllUseCase.ExecuteAsync();
        return Ok(cards);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var card = await _getByIdUseCase.ExecuteAsync(id);
            return Ok(card);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var card = await _createUseCase.ExecuteAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCreditCardDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var card = await _updateUseCase.ExecuteAsync(id, dto);
            return Ok(card);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _deleteUseCase.ExecuteAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/transactions")]
    public async Task<IActionResult> GetTransactions(int id)
    {
        try
        {
            var transactions = await _getTransactionsUseCase.ExecuteAsync(id);
            return Ok(transactions);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
