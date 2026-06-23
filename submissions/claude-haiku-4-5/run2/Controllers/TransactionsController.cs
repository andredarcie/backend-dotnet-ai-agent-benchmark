using CreditCardApi.DTOs;
using CreditCardApi.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(
    GetAllTransactionsUseCase getAllTransactionsUseCase,
    GetTransactionByIdUseCase getTransactionByIdUseCase,
    CreateTransactionUseCase createTransactionUseCase,
    UpdateTransactionUseCase updateTransactionUseCase,
    DeleteTransactionUseCase deleteTransactionUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var transactions = await getAllTransactionsUseCase.ExecuteAsync();
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var transaction = await getTransactionByIdUseCase.ExecuteAsync(id);
        if (transaction == null)
            return NotFound();

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        try
        {
            var transaction = await createTransactionUseCase.ExecuteAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionDto dto)
    {
        try
        {
            var transaction = await updateTransactionUseCase.ExecuteAsync(id, dto);
            if (transaction == null)
                return NotFound();

            return Ok(transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await deleteTransactionUseCase.ExecuteAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
