using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RedditApp.Models
{
    public class RedditPost
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Url { get; set; }
        public int Score { get; set; }
        public int Ups { get; set; }
        public int Downs { get; set; }
        public string Permalink { get; set; }
       
        public string Selftext { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int NumComments { get; set; }


    }
}

