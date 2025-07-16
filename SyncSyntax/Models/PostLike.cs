using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SyncSyntax.Models;

public class PostLike
{
    public int Id { get; set; }


    [DataType(DataType.DateTime)]
    [Required(ErrorMessage = "LikedAt is required.")]
    public DateTime LikedAt { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "PostId is required.")]
    [ForeignKey("Post")]
    public int PostId { get; set; }
    public Post Post { get; set; }

    [Required(ErrorMessage = "UserId is required.")]
    public string UserId { get; set; }
    public AppUser User { get; set; }

}