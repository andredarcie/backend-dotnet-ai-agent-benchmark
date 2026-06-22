using CreditCardApi.Application.Common;
using CreditCardApi.Application.UseCases.CreditCards;
using CreditCardApi.Contracts.CreditCards;
using CreditCardApi.Contracts.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("api/credit-cards")]
public sealed class CreditCardsController(
    GetAllCreditCardsUseCase getAllUseCase,
    GetCreditCardByIdUseCase getByIdUseCase,
    CreateCreditCardUseCase createUseCase,
    UpdateCreditCardUseCase updateUseCase,
    DeleteCreditCardUseCase deleteUseCase,
    GetCreditCardTransactionsUseCase getTransactionsUseCase) : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy" });

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var response = await getAllUseCase.ExecuteAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CreditCardResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var response = await getByIdUseCase.ExecuteAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<CreditCardResponse>> Create(
        [FromBody] CreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createUseCase.ExecuteAsync(request, cancellationToken);

        if (result.Type == UseCaseResultType.ValidationError)
            return BadRequest(new { errors = result.Errors });

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CreditCardResponse>> Update(
        int id,
        [FromBody] CreditCardRequest request,
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

    [HttpGet("{id:int}/transactions")]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetTransactions(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await getTransactionsUseCase.ExecuteAsync(id, cancellationToken);
        return result.Type == UseCaseResultType.NotFound ? NotFound() : Ok(result.Value);
    }
}
