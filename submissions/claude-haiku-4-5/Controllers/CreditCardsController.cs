using CreditCardApi.DTOs;
using CreditCardApi.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/credit-cards")]
public class CreditCardsController : ControllerBase
{
    private readonly CreateCreditCardUseCase _createUseCase;
    private readonly GetCreditCardByIdUseCase _getByIdUseCase;
    private readonly GetAllCreditCardsUseCase _getAllUseCase;
    private readonly UpdateCreditCardUseCase _updateUseCase;
    private readonly DeleteCreditCardUseCase _deleteUseCase;
    private readonly GetCreditCardTransactionsUseCase _getTransactionsUseCase;

    public CreditCardsController(
        CreateCreditCardUseCase createUseCase,
        GetCreditCardByIdUseCase getByIdUseCase,
        GetAllCreditCardsUseCase getAllUseCase,
        UpdateCreditCardUseCase updateUseCase,
        DeleteCreditCardUseCase deleteUseCase,
        GetCreditCardTransactionsUseCase getTransactionsUseCase)
    {
        _createUseCase = createUseCase;
        _getByIdUseCase = getByIdUseCase;
        _getAllUseCase = getAllUseCase;
        _updateUseCase = updateUseCase;
        _deleteUseCase = deleteUseCase;
        _getTransactionsUseCase = getTransactionsUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CreditCardResponse>>> GetAll()
    {
        var result = await _getAllUseCase.ExecuteAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CreditCardResponse>> GetById(int id)
    {
        var result = await _getByIdUseCase.ExecuteAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreditCardResponse>> Create(CreateCreditCardRequest request)
    {
        try
        {
            var result = await _createUseCase.ExecuteAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CreditCardResponse>> Update(int id, CreateCreditCardRequest request)
    {
        try
        {
            var result = await _updateUseCase.ExecuteAsync(id, request);
            if (result == null)
                return NotFound();
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _deleteUseCase.ExecuteAsync(id);
        if (!result)
            return NotFound();
        return NoContent();
    }

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetTransactions(int id)
    {
        var result = await _getTransactionsUseCase.ExecuteAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}
