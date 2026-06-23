using CreditCardApi.DTOs;
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var transaction = await _getByIdUseCase.ExecuteAsync(id);
        if (transaction == null) return NotFound();
        return Ok(transaction);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        var (transaction, error) = await _createUseCase.ExecuteAsync(dto);
        if (error != null) return BadRequest(new { error });
        return CreatedAtAction(nameof(GetById), new { id = transaction!.Id }, transaction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionDto dto)
    {
        var (transaction, error, notFound) = await _updateUseCase.ExecuteAsync(id, dto);
        if (notFound) return NotFound();
        if (error != null) return BadRequest(new { error });
        return Ok(transaction);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var found = await _deleteUseCase.ExecuteAsync(id);
        if (!found) return NotFound();
        return NoContent();
    }
}
