using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditApp.Models
{
    public class RedditPostData
    {
        public string Title { get; set; }
        public int Ups { get; set; }
        public int Downs { get; set; }
        public string Permalink { get; set; }
        public string Url { get; set; }
        public string Selftext { get; set; }
        public string Author { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int NumComments { get; set; }
    }
}
