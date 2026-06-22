using CreditCardApi.DTOs;
using CreditCardApi.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly CreateTransactionUseCase _createUseCase;
    private readonly GetTransactionByIdUseCase _getByIdUseCase;
    private readonly GetAllTransactionsUseCase _getAllUseCase;
    private readonly UpdateTransactionUseCase _updateUseCase;
    private readonly DeleteTransactionUseCase _deleteUseCase;

    public TransactionsController(
        CreateTransactionUseCase createUseCase,
        GetTransactionByIdUseCase getByIdUseCase,
        GetAllTransactionsUseCase getAllUseCase,
        UpdateTransactionUseCase updateUseCase,
        DeleteTransactionUseCase deleteUseCase)
    {
        _createUseCase = createUseCase;
        _getByIdUseCase = getByIdUseCase;
        _getAllUseCase = getAllUseCase;
        _updateUseCase = updateUseCase;
        _deleteUseCase = deleteUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetAll()
    {
        var result = await _getAllUseCase.ExecuteAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponse>> GetById(int id)
    {
        var result = await _getByIdUseCase.ExecuteAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(CreateTransactionRequest request)
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
    public async Task<ActionResult<TransactionResponse>> Update(int id, CreateTransactionRequest request)
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
}
