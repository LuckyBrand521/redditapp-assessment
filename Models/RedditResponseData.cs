using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditApp.Models
{
    public class RedditResponseData
    {
        public IEnumerable<RedditPostData> Children { get; set; }
        public int Dist { get; set; }
        public string After { get; set; }
        public string Before { get; set; }
    }
}
