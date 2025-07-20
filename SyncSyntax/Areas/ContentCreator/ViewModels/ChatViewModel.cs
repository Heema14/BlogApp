using SyncSyntax.Models;

public class ChatViewModel
{
    public string CurrentUserId { get; set; }   // أضيفي هذا الحقل

    public List<Message> Conversations { get; set; }
    public List<AppUser> OtherUsers { get; set; }
    public List<AppUser> AvailableUsers { get; set; }
}