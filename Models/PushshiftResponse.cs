using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditApp.Models
{
    public class PushshiftResponse<T>
    {
        public IEnumerable<T> Data { get; set; }
    }
}
