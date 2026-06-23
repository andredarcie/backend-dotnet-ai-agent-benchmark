using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CreditCardApi.DTOs;
using CreditCardApi.UseCases.CreditCards;

namespace CreditCardApi.Controllers
{
    [ApiController]
    [Route("api/credit-cards")]
    public class CreditCardsController : ControllerBase
    {
        private readonly GetCreditCardsUseCase _getCreditCardsUseCase;
        private readonly GetCreditCardByIdUseCase _getCreditCardByIdUseCase;
        private readonly CreateCreditCardUseCase _createCreditCardUseCase;
        private readonly UpdateCreditCardUseCase _updateCreditCardUseCase;
        private readonly DeleteCreditCardUseCase _deleteCreditCardUseCase;
        private readonly GetCreditCardTransactionsUseCase _getCreditCardTransactionsUseCase;

        public CreditCardsController(
            GetCreditCardsUseCase getCreditCardsUseCase,
            GetCreditCardByIdUseCase getCreditCardByIdUseCase,
            CreateCreditCardUseCase createCreditCardUseCase,
            UpdateCreditCardUseCase updateCreditCardUseCase,
            DeleteCreditCardUseCase deleteCreditCardUseCase,
            GetCreditCardTransactionsUseCase getCreditCardTransactionsUseCase)
        {
            _getCreditCardsUseCase = getCreditCardsUseCase;
            _getCreditCardByIdUseCase = getCreditCardByIdUseCase;
            _createCreditCardUseCase = createCreditCardUseCase;
            _updateCreditCardUseCase = updateCreditCardUseCase;
            _deleteCreditCardUseCase = deleteCreditCardUseCase;
            _getCreditCardTransactionsUseCase = getCreditCardTransactionsUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _getCreditCardsUseCase.ExecuteAsync();
            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _getCreditCardByIdUseCase.ExecuteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }
            return Ok(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCreditCardRequest request)
        {
            var result = await _createCreditCardUseCase.ExecuteAsync(request);
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCreditCardRequest request)
        {
            var result = await _updateCreditCardUseCase.ExecuteAsync(id, request);
            if (result.NotFound)
            {
                return NotFound();
            }
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _deleteCreditCardUseCase.ExecuteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpGet("{id}/transactions")]
        public async Task<IActionResult> GetTransactions(int id)
        {
            var result = await _getCreditCardTransactionsUseCase.ExecuteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }
            return Ok(result.Value);
        }
    }
}
