using ElasticSearchWebAppTest.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearchWebAppTest.Data
{
    public class JsonDataConvertor
    {
        public List<Post.RootObject> JsonToPostList()
        {
            string dataPath = @"Data\json_test_data.txt";
            string json = File.ReadAllText(dataPath);
            var listWithPostRoots = JsonConvert.DeserializeObject<List<Post.RootObject>>(json);

            return listWithPostRoots;
        }
    }
}
