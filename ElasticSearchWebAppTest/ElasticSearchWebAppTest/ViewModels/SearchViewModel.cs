using ElasticSearchWebAppTest.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearchWebAppTest.ViewModels
{
    public class SearchViewModel
    {
        public string SearchString { get; set; }
        public IEnumerable<Post> Posts { get; set; }
        public int AmountOfHits { get; set; }
        //public IEnumerable<string> Tags { get; set; }
        public string SelectedTag { get; set; }
        public IEnumerable<string> SelectedTags { get; set; }
        public IEnumerable<SelectListItem> AvailableTags { get; set; }
        public IEnumerable<string> SelectedCategories { get; set; }
        public IEnumerable<SelectListItem> AvailableCategories { get; set; }
        public IEnumerable<string> SelectedSubCategories { get; set; }
        public IEnumerable<SelectListItem> AvailableSubCategories { get; set; }

        public SearchViewModel()
        {
            Posts = new List<Post>();
            SelectedTags = new List<string>();
            AvailableTags = new List<SelectListItem>();
            SelectedCategories = new List<string>();
            AvailableCategories = new List<SelectListItem>();
            SelectedSubCategories = new List<string>();
            AvailableSubCategories = new List<SelectListItem>();
        }
        //public string SelectedTag { get; set; }
        //public IEnumerable<string> SelectedTags { get; set; }
        //public Dictionary<string, long> AggregationsByTags { get; set; }
        //public List<KeyValuePair<string, long>> AggregationsByTags { get; set; }
        //public IEnumerable<string> AggregationsByTags { get; set; }

    }
}
