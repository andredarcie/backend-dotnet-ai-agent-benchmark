using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.UseCases.CreditCards;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/credit-cards")]
public class CreditCardsController : ControllerBase
{
    private readonly GetAllCreditCardsUseCase _getAll;
    private readonly GetCreditCardByIdUseCase _getById;
    private readonly CreateCreditCardUseCase _create;
    private readonly UpdateCreditCardUseCase _update;
    private readonly DeleteCreditCardUseCase _delete;
    private readonly GetCreditCardTransactionsUseCase _getTransactions;

    public CreditCardsController(
        GetAllCreditCardsUseCase getAll,
        GetCreditCardByIdUseCase getById,
        CreateCreditCardUseCase create,
        UpdateCreditCardUseCase update,
        DeleteCreditCardUseCase delete,
        GetCreditCardTransactionsUseCase getTransactions)
    {
        _getAll = getAll;
        _getById = getById;
        _create = create;
        _update = update;
        _delete = delete;
        _getTransactions = getTransactions;
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
    public async Task<IActionResult> Create([FromBody] CreateCreditCardRequest request, CancellationToken ct)
    {
        var result = await _create.ExecuteAsync(request, ct);
        if (result.Status == ResultStatus.Invalid)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCreditCardRequest request, CancellationToken ct)
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

    [HttpGet("{id:int}/transactions")]
    public async Task<IActionResult> GetTransactions(int id, CancellationToken ct)
    {
        var result = await _getTransactions.ExecuteAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
