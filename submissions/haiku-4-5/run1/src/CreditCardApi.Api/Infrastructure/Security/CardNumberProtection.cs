namespace CreditCardApi.Api.Infrastructure.Security;

public static class CardNumberProtection
{
    public static string TruncateCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
            return "****";

        return $"****-****-****-{cardNumber[^4..]}";
    }

    public static bool IsValidCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || !cardNumber.All(char.IsDigit))
            return false;

        if (cardNumber.Length < 13 || cardNumber.Length > 19)
            return false;

        return LuhnCheck(cardNumber);
    }

    private static bool LuhnCheck(string cardNumber)
    {
        int sum = 0;
        bool isEven = false;

        for (int i = cardNumber.Length - 1; i >= 0; i--)
        {
            int digit = cardNumber[i] - '0';

            if (isEven)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            isEven = !isEven;
        }

        return sum % 10 == 0;
    }
}
