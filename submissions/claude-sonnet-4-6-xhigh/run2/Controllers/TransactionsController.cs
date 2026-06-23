using CreditCardApi.DTOs;
using CreditCardApi.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly GetAllTransactionsUseCase _getAll;
    private readonly GetTransactionByIdUseCase _getById;
    private readonly CreateTransactionUseCase _create;
    private readonly UpdateTransactionUseCase _update;
    private readonly DeleteTransactionUseCase _delete;

    public TransactionsController(
        GetAllTransactionsUseCase getAll,
        GetTransactionByIdUseCase getById,
        CreateTransactionUseCase create,
        UpdateTransactionUseCase update,
        DeleteTransactionUseCase delete)
    {
        _getAll = getAll;
        _getById = getById;
        _create = create;
        _update = update;
        _delete = delete;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var transactions = await _getAll.ExecuteAsync();
        return Ok(transactions.Select(t => new TransactionResponse
        {
            Id = t.Id,
            CreditCardId = t.CreditCardId,
            Amount = t.Amount,
            Merchant = t.Merchant,
            Category = t.Category,
            CreatedAt = t.CreatedAt
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var transaction = await _getById.ExecuteAsync(id);
        if (transaction is null) return NotFound();
        return Ok(new TransactionResponse
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        var (transaction, error) = await _create.ExecuteAsync(request);
        if (error is not null) return BadRequest(new { error });
        var response = new TransactionResponse
        {
            Id = transaction!.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt
        };
        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request)
    {
        var (transaction, error, notFound) = await _update.ExecuteAsync(id, request);
        if (notFound) return NotFound();
        if (error is not null) return BadRequest(new { error });
        return Ok(new TransactionResponse
        {
            Id = transaction!.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _delete.ExecuteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
