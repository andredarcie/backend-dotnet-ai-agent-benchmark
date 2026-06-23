using CreditCardApi.DTOs;
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
    private readonly GetTransactionsByCardIdUseCase _getTransactionsUseCase;

    public CreditCardsController(
        GetAllCreditCardsUseCase getAllUseCase,
        GetCreditCardByIdUseCase getByIdUseCase,
        CreateCreditCardUseCase createUseCase,
        UpdateCreditCardUseCase updateUseCase,
        DeleteCreditCardUseCase deleteUseCase,
        GetTransactionsByCardIdUseCase getTransactionsUseCase)
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var card = await _getByIdUseCase.ExecuteAsync(id);
        if (card == null) return NotFound();
        return Ok(card);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardDto dto)
    {
        var (card, error) = await _createUseCase.ExecuteAsync(dto);
        if (error != null) return BadRequest(new { error });
        return CreatedAtAction(nameof(GetById), new { id = card!.Id }, card);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCreditCardDto dto)
    {
        var (card, error, notFound) = await _updateUseCase.ExecuteAsync(id, dto);
        if (notFound) return NotFound();
        if (error != null) return BadRequest(new { error });
        return Ok(card);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var found = await _deleteUseCase.ExecuteAsync(id);
        if (!found) return NotFound();
        return NoContent();
    }

    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetTransactions(int id)
    {
        var (transactions, notFound) = await _getTransactionsUseCase.ExecuteAsync(id);
        if (notFound) return NotFound();
        return Ok(transactions);
    }
}
