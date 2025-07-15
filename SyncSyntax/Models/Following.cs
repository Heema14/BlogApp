using SyncSyntax.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Following
{
    [Key]
    public int Id { get; set; }

    public string FollowerId { get; set; }  // ID المستخدم الذي يتابع
    public string FollowingId { get; set; }  // ID المستخدم الذي يتم متابعته

    [ForeignKey("FollowerId")]
    public AppUser Follower { get; set; }

    [ForeignKey("FollowingId")]
    public AppUser FollowedUser { get; set; } // اسم المستخدم الذي يتم متابعته
}
