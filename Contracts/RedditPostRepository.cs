using RedditApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedditApp.Contracts
{
    public class RedditPostRepository : IRedditPostRepository
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RedditPostRepository> _logger;
        private readonly SemaphoreSlim _rateLimitSemaphore;
        private int _rateLimitRemaining;
        private DateTimeOffset _rateLimitReset;

        public RedditPostRepository(HttpClient httpClient, ILogger<RedditPostRepository> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _rateLimitSemaphore = new SemaphoreSlim(1, 1);
        }

        private async Task<int> AcquireRateLimitTokenAsync()
        {
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                // Check if the rate limit has been reached
                if (_rateLimitRemaining <= 0 && DateTimeOffset.UtcNow < _rateLimitReset)
                {
                    // Calculate the delay before the next request can be made
                    var delay = _rateLimitReset - DateTimeOffset.UtcNow;
                    _logger.LogInformation($"Rate limit reached, waiting {delay.TotalSeconds} seconds before retrying.");
                    await Task.Delay(delay);
                }

                return _rateLimitRemaining;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }

        private void UpdateRateLimitHeaders(HttpResponseHeaders headers)
        {
            if (headers.TryGetValues("X-Ratelimit-Remaining", out var rateLimitRemainingValues))
            {
                int.TryParse(rateLimitRemainingValues.FirstOrDefault(), out _rateLimitRemaining);
            }

            if (headers.TryGetValues("X-Ratelimit-Reset", out var rateLimitResetValues))
            {
                if (long.TryParse(rateLimitResetValues.FirstOrDefault(), out var rateLimitResetSeconds))
                {
                    _rateLimitReset = DateTimeOffset.UtcNow.AddSeconds(rateLimitResetSeconds);
                }
            }
        }

        public async Task<IEnumerable<RedditPost>> GetTopPostsFromSubredditAsync(string subredditName, int limit, CancellationToken cancellationToken)
        {
            var url = $"https://api.pushshift.io/reddit/submission/search?subreddit={subredditName}&sort=score&sort_type=desc&size={limit}";

            try
            {
                var response = await SendRequestWithRateLimitAsync(() => _httpClient.GetAsync(url, cancellationToken), cancellationToken);
                response.EnsureSuccessStatusCode();

                UpdateRateLimitHeaders(response.Headers);

                var data = await JsonSerializer.DeserializeAsync<PushshiftResponse<RedditPost>>(await response.Content.ReadAsStreamAsync(), cancellationToken: cancellationToken);
                return data?.Data ?? Enumerable.Empty<RedditPost>();
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("Rate limit reached"))
            {
                _logger.LogWarning(ex, "Rate limit reached for Pushshift.io API");
                throw new Exception("Rate limit reached for Pushshift.io API", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top posts from Pushshift.io API");
                throw;
            }
        }





        private async Task<HttpResponseMessage> SendRequestWithRateLimitAsync(Func<Task<HttpResponseMessage>> requestFunc, CancellationToken cancellationToken)
        {
            var rateLimitRemaining = await AcquireRateLimitTokenAsync();
            if (rateLimitRemaining > 0)
            {
                return await requestFunc();
            }
            else
            {
                throw new HttpRequestException("Rate limit reached, unable to process request.", null, System.Net.HttpStatusCode.TooManyRequests);
            }
        }



        public async Task<IEnumerable<UserPostCount>> GetTopUserPostCountsAsync(string subredditName, int topCount, CancellationToken cancellationToken)
        {
            var url = $"https://api.pushshift.io/reddit/submission/search?subreddit={subredditName}&sort=score&sort_type=desc&size={int.MaxValue}";

            try
            {
                var response = await SendRequestWithRateLimitAsync(() => _httpClient.GetAsync(url, cancellationToken), cancellationToken);
                response.EnsureSuccessStatusCode();

                UpdateRateLimitHeaders(response.Headers);

                var data = await JsonSerializer.DeserializeAsync<PushshiftResponse<RedditPost>>(await response.Content.ReadAsStreamAsync(), cancellationToken: cancellationToken);
                var userPostCounts = data?.Data
                                     .GroupBy(p => p.Author)
                                     .Select(g => new UserPostCount
                                     {
                                         Author = g.Key,
                                         PostCount = g.Count()
                                     })
                                     .OrderByDescending(x => x.PostCount)
                                     .Take(topCount)
                                     .ToList();

                return userPostCounts;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("Rate limit reached"))
            {
                _logger.LogWarning(ex, "Rate limit reached for Pushshift.io API");
                throw new Exception("Rate limit reached for Pushshift.io API", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top user post counts from Pushshift.io API");
                throw;
            }
        }

        private async Task<T> SendRequestWithRateLimitAsync<T>(Func<Task<T>> apiCall, CancellationToken cancellationToken, int maxRetries = 3, int retryDelaySeconds = 5)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    return await apiCall();
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("Rate limit reached"))
                {
                    if (++retryCount > maxRetries)
                    {
                        throw;
                    }

                    Console.WriteLine($"Rate limit reached, retrying in {retryDelaySeconds} seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
                }
            }
        }

    }
}