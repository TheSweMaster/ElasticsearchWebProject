using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElasticSearchWebAppTest.Models;
using Newtonsoft.Json;
using System.IO;
using Nest;

namespace ElasticSearchWebAppTest.Data
{
    public class ElasticSearchContext : IElasticSearchContext
    {
        // TODO Fix singelton here
        private static Uri node;
        public ElasticClient GetClient()
        {
            node = new Uri("http://localhost:9200");
            settings.DefaultIndex("my_blog");
            var client = new ElasticClient(settings);

            return client;
        }

        private static readonly ConnectionSettings settings = new ConnectionSettings();

        private ElasticSearchContext() { }

        public static ConnectionSettings Settings
        {
            get
            {
                return new ConnectionSettings(node);
            }
        }


    }
}
