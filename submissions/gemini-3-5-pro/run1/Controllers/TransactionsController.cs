using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CreditCardApi.DTOs;
using CreditCardApi.UseCases.Transactions;

namespace CreditCardApi.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly GetTransactionsUseCase _getTransactionsUseCase;
        private readonly GetTransactionByIdUseCase _getTransactionByIdUseCase;
        private readonly CreateTransactionUseCase _createTransactionUseCase;
        private readonly UpdateTransactionUseCase _updateTransactionUseCase;
        private readonly DeleteTransactionUseCase _deleteTransactionUseCase;

        public TransactionsController(
            GetTransactionsUseCase getTransactionsUseCase,
            GetTransactionByIdUseCase getTransactionByIdUseCase,
            CreateTransactionUseCase createTransactionUseCase,
            UpdateTransactionUseCase updateTransactionUseCase,
            DeleteTransactionUseCase deleteTransactionUseCase)
        {
            _getTransactionsUseCase = getTransactionsUseCase;
            _getTransactionByIdUseCase = getTransactionByIdUseCase;
            _createTransactionUseCase = createTransactionUseCase;
            _updateTransactionUseCase = updateTransactionUseCase;
            _deleteTransactionUseCase = deleteTransactionUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _getTransactionsUseCase.ExecuteAsync();
            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _getTransactionByIdUseCase.ExecuteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }
            return Ok(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
        {
            var result = await _createTransactionUseCase.ExecuteAsync(request);
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request)
        {
            var result = await _updateTransactionUseCase.ExecuteAsync(id, request);
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
            var result = await _deleteTransactionUseCase.ExecuteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
