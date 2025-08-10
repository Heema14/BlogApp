using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;

public class ArchiveService
{
    private readonly AppDbContext _context;

    public ArchiveService(AppDbContext context)
    {
        _context = context;
    }

    public async Task ArchiveOldMessages()
    {
        Console.WriteLine("📦 Archive job started...");

        var oldMessages = await _context.Messages
            .Where(m => m.SentAt < DateTime.UtcNow.AddDays(-360))
            .ToListAsync();

        if (!oldMessages.Any())
        {
            Console.WriteLine("🔍 No messages to archive.");
            return;
        }

        var archived = oldMessages.Select(m => new ArchivedMessage
        {
            SenderId = m.SenderId,
            ReceiverId = m.ReceiverId,
            Content = m.Content,
            SentAt = m.SentAt,
            IsRead = m.IsRead,
            ReadAt = m.ReadAt,
            IsPinned = m.IsPinned
        }).ToList();

        _context.ArchivedMessages.AddRange(archived);
        _context.Messages.RemoveRange(oldMessages);

        await _context.SaveChangesAsync();

        Console.WriteLine($"✅ Archived {archived.Count} messages successfully");
    }

}
 
