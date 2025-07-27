using SyncSyntax.Models;
using System.ComponentModel.DataAnnotations;

public class MessageReaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required]
    public int MessageId { get; set; }

    [Required]
    public string Reaction { get; set; }

    public DateTime ReactedAt { get; set; } = DateTime.Now;

    public virtual Message Message { get; set; }

    // 👇 أضف هذا
    public virtual AppUser User { get; set; }
}
