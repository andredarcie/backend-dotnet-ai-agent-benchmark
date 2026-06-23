using Microsoft.AspNetCore.Mvc;
using CreditCardApi.DTOs;
using CreditCardApi.UseCases;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly GetAllTransactionsUseCase _getAllUseCase;
    private readonly GetTransactionByIdUseCase _getByIdUseCase;
    private readonly CreateTransactionUseCase _createUseCase;
    private readonly UpdateTransactionUseCase _updateUseCase;
    private readonly DeleteTransactionUseCase _deleteUseCase;

    public TransactionsController(
        GetAllTransactionsUseCase getAllUseCase,
        GetTransactionByIdUseCase getByIdUseCase,
        CreateTransactionUseCase createUseCase,
        UpdateTransactionUseCase updateUseCase,
        DeleteTransactionUseCase deleteUseCase)
    {
        _getAllUseCase = getAllUseCase;
        _getByIdUseCase = getByIdUseCase;
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _deleteUseCase = deleteUseCase;
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
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
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
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request)
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
}
