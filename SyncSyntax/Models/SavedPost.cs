using SyncSyntax.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class SavedPost
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }

    [ForeignKey("UserId")]
    public AppUser User { get; set; }

    [Required]
    public int PostId { get; set; }

    [ForeignKey("PostId")]
    public Post Post { get; set; }

    public DateTime SavedAt { get; set; } = DateTime.Now;
}
