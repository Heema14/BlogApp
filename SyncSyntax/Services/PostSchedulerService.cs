using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;   

public class PostSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PostSchedulerService> _logger;

    public PostSchedulerService(IServiceScopeFactory serviceScopeFactory, ILogger<PostSchedulerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(60000, stoppingToken);  

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var postsToPublish = await _context.Posts
                    .Where(p => p.PublishDate.HasValue && p.PublishDate <= DateTime.Now && !p.IsPublished)
                    .ToListAsync(stoppingToken);  

                foreach (var post in postsToPublish)
                {
                    post.IsPublished = true;
                    _context.Update(post);
                }

                await _context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Checked for posts to publish.");
            }
        }
    }
}
