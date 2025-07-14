using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SyncSyntax.Models;

public class PostLike
{
    public int Id { get; set; }

    [Required(ErrorMessage = "PostId is required.")]
    [ForeignKey("Post")]
    public int PostId { get; set; }

    [Required(ErrorMessage = "UserId is required.")]
    public string UserId { get; set; }

    [DataType(DataType.DateTime)]
    [Required(ErrorMessage = "LikedAt is required.")]
    public DateTime LikedAt { get; set; } = DateTime.Now;

    public Post Post { get; set; }  // العلاقة مع المقال
    public AppUser User { get; set; }  // العلاقة مع المستخدم
}
