using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CreditCardApi.Models;

public class Transaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int CreditCardId { get; set; }

    [Required]
    [Column(TypeName = "numeric")]
    public decimal Amount { get; set; }

    [Required]
    public string Merchant { get; set; } = null!;

    public string? Category { get; set; }

    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(CreditCardId))]
    public CreditCard CreditCard { get; set; } = null!;
}
