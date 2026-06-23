using CreditCardApi.DTOs;
using CreditCardApi.Exceptions;
using CreditCardApi.UseCases.Transactions;
using Microsoft.AspNetCore.Mvc;

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
        var transactions = await _getAllUseCase.ExecuteAsync();
        return Ok(transactions);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var transaction = await _getByIdUseCase.ExecuteAsync(id);
            return Ok(transaction);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var transaction = await _createUseCase.ExecuteAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var transaction = await _updateUseCase.ExecuteAsync(id, dto);
            return Ok(transaction);
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
}
