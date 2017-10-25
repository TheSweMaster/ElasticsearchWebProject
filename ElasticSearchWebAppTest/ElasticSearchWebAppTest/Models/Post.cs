using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearchWebAppTest.Models
{
    public class Post
    {
        public int UserId { get; set; }
        public string Title { get; set; }
        [Keyword]
        public IEnumerable<string> Tags { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public DateTime PostDate { get; set; }
        public string PostText { get; set; }
        //[Completion]
        //public IEnumerable<string> Suggest { get; set; }
        [Completion]
        public CompletionField Suggest { get; set; }

        public Post()
        {
            Tags = new List<string>();
            //Categories = new List<string>();
            Suggest = new CompletionField();
        }

        public class RootObject
        {
            public int userId { get; set; }
            public string title { get; set; }
            public List<string> tags { get; set; }
            public string category { get; set; }
            public string subCategory { get; set; }
            public string postDate { get; set; }
            public string postText { get; set; }
            public string suggest { get; set; }
        }

    }
}
