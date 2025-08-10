using System.ComponentModel.DataAnnotations;

public class Message
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; }

    [Required]
    public string ReceiverId { get; set; }

    [Required]
    public string Content { get; set; }

    public DateTime SentAt { get; set; } = DateTime.Now;

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public string? AudioPath { get; set; }
    public bool IsPinned { get; set; } = false;

}
