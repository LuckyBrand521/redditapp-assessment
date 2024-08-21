using RedditApp.Contracts;

public class RedditStatisticsBackgroundService : BackgroundService
{
    private readonly IRedditPostRepository _redditPostRepository;
    private readonly ILogger<RedditStatisticsBackgroundService> _logger;

    public RedditStatisticsBackgroundService(IRedditPostRepository redditPostRepository, ILogger<RedditStatisticsBackgroundService> logger)
    {
        _redditPostRepository = redditPostRepository;
        _logger = logger;
    }

    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Get the top 10 posts and top 10 users by post count for the "python" subreddit
                var topPosts = await _redditPostRepository.GetTopPostsFromSubredditAsync("python", 10, stoppingToken);
                var topUserPostCounts = await _redditPostRepository.GetTopUserPostCountsAsync("python", 10, stoppingToken);

                // Log the results
                _logger.LogInformation("Top 10 Posts by Score:");
                foreach (var post in topPosts)
                {
                    _logger.LogInformation($"Title: {post.Title} | Score: {post.Score} | Author: {post.Author}");
                }

                _logger.LogInformation("\nTop 10 Users by Post Count:");
                foreach (var userPostCount in topUserPostCounts)
                {
                    _logger.LogInformation($"Author: {userPostCount.Author} | Post Count: {userPostCount.PostCount}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving Reddit statistics.");
            }

            // Wait for 1 hour before checking again
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
