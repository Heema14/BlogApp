public class PostWithFollowStatusViewModel
{
    public Post Post { get; set; }
    public bool IsFollowing { get; set; }

    public bool UserLikedPost { get; set; }
    public bool IsSaved { get; set; }
}
