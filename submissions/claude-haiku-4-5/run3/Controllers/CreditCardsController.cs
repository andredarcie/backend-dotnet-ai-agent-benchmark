using Microsoft.AspNetCore.Mvc;
using CreditCardApi.DTOs;
using CreditCardApi.UseCases;

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
        var result = await _getAllUseCase.ExecuteAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _getByIdUseCase.ExecuteAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardRequest request)
    {
        try
        {
            var result = await _createUseCase.ExecuteAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCreditCardRequest request)
    {
        try
        {
            var result = await _updateUseCase.ExecuteAsync(id, request);
            if (result == null)
                return NotFound();
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return BadRequest();
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
    public async Task<IActionResult> GetTransactions(int id)
    {
        var result = await _getTransactionsUseCase.ExecuteAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}
