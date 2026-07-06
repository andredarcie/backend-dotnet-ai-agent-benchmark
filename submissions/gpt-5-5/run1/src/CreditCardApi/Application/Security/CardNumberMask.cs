namespace CreditCardApi.Application.Security;

public static class CardNumberMask
{
    public static string Mask(string last4) => $"************{last4}";
}
