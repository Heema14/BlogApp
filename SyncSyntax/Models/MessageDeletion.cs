public class MessageDeletion
{
    public int Id { get; set; }

    public string UserId { get; set; }
    public int MessageId { get; set; }

    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;

    // علاقات (اختياري)
    public Message Message { get; set; }
}
