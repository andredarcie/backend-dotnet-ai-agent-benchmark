using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CreditCardApi.Models;

public class CreditCard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string CardholderName { get; set; } = null!;

    [Required]
    public string CardNumber { get; set; } = null!;

    public string? Brand { get; set; }

    [Required]
    [Column(TypeName = "numeric")]
    public decimal CreditLimit { get; set; }

    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
