using CreditCardApi.Application.Common;
using CreditCardApi.Application.UseCases.Transactions;
using CreditCardApi.Contracts.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController(
    GetAllTransactionsUseCase getAllUseCase,
    GetTransactionByIdUseCase getByIdUseCase,
    CreateTransactionUseCase createUseCase,
    UpdateTransactionUseCase updateUseCase,
    DeleteTransactionUseCase deleteUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var response = await getAllUseCase.ExecuteAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var response = await getByIdUseCase.ExecuteAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(
        [FromBody] TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createUseCase.ExecuteAsync(request, cancellationToken);

        if (result.Type == UseCaseResultType.ValidationError)
            return BadRequest(new { errors = result.Errors });

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TransactionResponse>> Update(
        int id,
        [FromBody] TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateUseCase.ExecuteAsync(id, request, cancellationToken);

        return result.Type switch
        {
            UseCaseResultType.NotFound => NotFound(),
            UseCaseResultType.ValidationError => BadRequest(new { errors = result.Errors }),
            _ => Ok(result.Value)
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await deleteUseCase.ExecuteAsync(id, cancellationToken);
        return result.Type == UseCaseResultType.NotFound ? NotFound() : NoContent();
    }
}
