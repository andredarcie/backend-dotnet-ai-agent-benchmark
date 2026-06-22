using System.Threading.Tasks;
using Gemini.Data.Repositories;

namespace Gemini.UseCases;

public class DeleteCreditCardUseCase
{
    private readonly ICreditCardRepository _creditCardRepository;

    public DeleteCreditCardUseCase(ICreditCardRepository creditCardRepository)
    {
        _creditCardRepository = creditCardRepository;
    }

    public async Task<bool> ExecuteAsync(int id)
    {
        var existingCard = await _creditCardRepository.GetByIdAsync(id);
        if (existingCard == null)
        {
            return false;
        }

        _creditCardRepository.Delete(existingCard);
        await _creditCardRepository.SaveChangesAsync();

        return true;
    }
}
