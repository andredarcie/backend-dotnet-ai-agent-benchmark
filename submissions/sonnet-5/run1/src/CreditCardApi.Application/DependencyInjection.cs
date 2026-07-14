using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCardApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services
        .AddScoped<ICreditCardService, CreditCardService>()
        .AddScoped<ITransactionService, TransactionService>();
}
