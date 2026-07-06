namespace CreditCardApi.Application.Abstractions;

public interface ICardNumberProtector
{
    string Protect(string cardNumber);

    string Last4(string cardNumber);
}
