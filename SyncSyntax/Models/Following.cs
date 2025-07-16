using SyncSyntax.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Following
{
    [Key]
    public int Id { get; set; }

    public string FollowerId { get; set; }  
    public string FollowingId { get; set; } 

    [ForeignKey("FollowerId")]
    public AppUser Follower { get; set; }

    [ForeignKey("FollowingId")]
    public AppUser FollowedUser { get; set; } 
}
