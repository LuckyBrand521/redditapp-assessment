using RedditApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditApp.Contracts
{
    public interface IRedditPostRepository
    {
       
        Task<IEnumerable<RedditPost>> GetTopPostsFromSubredditAsync(string subredditName, int limit, CancellationToken cancellationToken);

        Task<IEnumerable<UserPostCount>> GetTopUserPostCountsAsync(string subredditName, int topCount, CancellationToken cancellationToken);
    }
}
