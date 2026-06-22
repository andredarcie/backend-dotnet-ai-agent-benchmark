using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.UseCases.Transactions;
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
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _getAll.ExecuteAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _getById.ExecuteAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        var result = await _create.ExecuteAsync(request, ct);
        if (result.Status == ResultStatus.Invalid)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request, CancellationToken ct)
    {
        var result = await _update.ExecuteAsync(id, request, ct);
        return result.Status switch
        {
            ResultStatus.Success => Ok(result.Value),
            ResultStatus.NotFound => NotFound(),
            _ => BadRequest(new { error = result.Error })
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _delete.ExecuteAsync(id, ct);
        return result.IsSuccess ? NoContent() : NotFound();
    }
}
