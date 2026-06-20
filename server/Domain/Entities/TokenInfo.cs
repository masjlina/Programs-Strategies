using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared;

namespace Domain.Entities;

public class TokenInfo : BaseEntity
{
    [Required] public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))] public User User { get; set; }
    [Required] public string RefreshToken { get; set; } = string.Empty;
    [Required] public DateTime ExpiredAt { get; set; }
}