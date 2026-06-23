using System;
using System.Threading.Tasks;
using Gemini.Models;
using Gemini.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Gemini.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly GetAllTransactionsUseCase _getAllTransactionsUseCase;
    private readonly GetTransactionByIdUseCase _getTransactionByIdUseCase;
    private readonly CreateTransactionUseCase _createTransactionUseCase;
    private readonly UpdateTransactionUseCase _updateTransactionUseCase;
    private readonly DeleteTransactionUseCase _deleteTransactionUseCase;

    public TransactionsController(
        GetAllTransactionsUseCase getAllTransactionsUseCase,
        GetTransactionByIdUseCase getTransactionByIdUseCase,
        CreateTransactionUseCase createTransactionUseCase,
        UpdateTransactionUseCase updateTransactionUseCase,
        DeleteTransactionUseCase deleteTransactionUseCase)
    {
        _getAllTransactionsUseCase = getAllTransactionsUseCase;
        _getTransactionByIdUseCase = getTransactionByIdUseCase;
        _createTransactionUseCase = createTransactionUseCase;
        _updateTransactionUseCase = updateTransactionUseCase;
        _deleteTransactionUseCase = deleteTransactionUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var transactions = await _getAllTransactionsUseCase.ExecuteAsync();
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var transaction = await _getTransactionByIdUseCase.ExecuteAsync(id);
        if (transaction == null)
        {
            return NotFound();
        }
        return Ok(transaction);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Transaction transaction)
    {
        try
        {
            var createdTransaction = await _createTransactionUseCase.ExecuteAsync(transaction);
            return CreatedAtAction(nameof(GetById), new { id = createdTransaction.Id }, createdTransaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Transaction transaction)
    {
        try
        {
            var updated = await _updateTransactionUseCase.ExecuteAsync(id, transaction);
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
        var deleted = await _deleteTransactionUseCase.ExecuteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}
