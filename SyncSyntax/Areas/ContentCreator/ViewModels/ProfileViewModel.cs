using SyncSyntax.Models;

public class ProfileViewModel
{
    public AppUser User { get; set; }
    public int FollowersCount { get; set; }
    public int PostsCount { get; set; }
    public List<Post> Posts { get; set; }
}
