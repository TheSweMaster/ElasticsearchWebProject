using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ElasticSearchWebAppTest.ViewModels;
using ElasticSearchWebAppTest.Models;
using Nest;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using ElasticSearchWebAppTest.Data;

namespace ElasticSearchWebAppTest.Controllers
{
    public class SearchController : Controller
    {
        private readonly Uri _node;
        private static ConnectionSettings _settings;
        private static ElasticClient _client;

        public SearchController()
        {
            _node = new Uri("http://localhost:9200");
            _settings = new ConnectionSettings(_node);
            _settings.DefaultIndex("my_blog");
            _client = new ElasticClient(_settings);
            // Need to set up an instance/service of Elaticsearch before launching
            // TODO Add singelton on ConnectionSettings
        }

        public IActionResult Index()
        {

            if (_client.IndexExists("my_blog").Exists == false)
            {
                return NotFound("The indexing of 'my_blog' does not exist.");
            }

            var model = new SearchViewModel()
            {
                // add stuff to model
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Index([Bind("SearchString")] SearchViewModel model)
        {
            var result = _client.Search<Post>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Must(mu => mu
                            .MultiMatch(mp => mp
                                .Query(model.SearchString)
                                .Fields(f => f
                                    .Field(f1 => f1.Title, 3)
                                    .Field(f3 => f3.Category, 2)
                                    .Field(f4 => f4.SubCategory, 1.5)
                                    .Field(f2 => f2.Tags, 1.5)
                                    .Field(f5 => f5.PostText, 0.2)
                                )
                            )
                        )
                    )
                )
                .Size(20)
            );

            model.Posts = result.Documents;
            model.AmountOfHits = (int)result.Total;

            return View(model);
        }

        public async Task<IActionResult> ListAll()
        {
            var result = await _client.SearchAsync<Post>(s => s
                .Query(q => q.MatchAll())
                .Size(200)
                );

            IEnumerable<Post> listOfPosts = result.Documents;

            return View(listOfPosts);
        }

        public IActionResult FacetSearch()
        {
            var model = new SearchViewModel
            {
                AvailableCategories = GetAllCategories(),
                AvailableSubCategories = GetAllSubCategories()
            };

            return View(model);
        }

        //[Bind("SearchString,SelectedCategories,SelectedSubCategories")]
        [HttpPost]
        public IActionResult FacetSearch(string queryString, List<string> SelectedCategories, List<string> SelectedSubCategories, SearchViewModel model)
        {
            model.SelectedCategories = SelectedCategories;
            model.SelectedSubCategories = SelectedSubCategories;

            IEnumerable<string> userCats = model.SelectedCategories;
            IEnumerable<string> userSubCats = model.SelectedSubCategories;

            var result = GetResultFromIncludedCatsAndSubCats(userCats, userSubCats);

            model.Posts = result.Documents;
            model.AmountOfHits = (int)result.Total;

            var resultExcluded = GetResultFromExcludedCatsAndSubCats(userCats, userSubCats);

            List<SelectListItem> catsDisplayList = GetCategoryDisplayList(result, resultExcluded);
            List<SelectListItem> subCatsDisplayList = GetSubCategoryDisplayList(result, resultExcluded);

            model.AvailableCategories = catsDisplayList;
            model.AvailableSubCategories = subCatsDisplayList;

            return View(model);
        }

        private List<SelectListItem> GetSubCategoryDisplayList(ISearchResponse<Post> result, ISearchResponse<Post> resultExcluded)
        {
            var subCatsDictionaryIncluded = new Dictionary<string, int>();
            if (result.Aggs.Terms("agg_by_subcats_included") != null)
            {
                subCatsDictionaryIncluded = result.Aggs
                .Terms("agg_by_subcats_included")
                .Buckets
                .ToDictionary(k => k.Key, v => (int)v.DocCount);
            }

            var subCatsDictionaryFields = new Dictionary<string, int>();
            if (result.Aggs.Terms("agg_by_subcats_fields") != null)
            {
                subCatsDictionaryFields = result.Aggs
                    .Terms("agg_by_subcats_fields")
                    .Buckets
                    .ToDictionary(k => k.Key, v => (int)v.DocCount);
            }

            var subCatsDictionaryExcluded = new Dictionary<string, int>();
            if (resultExcluded.Aggs.Terms("agg_by_subcats_excluded") != null)
            {
                subCatsDictionaryExcluded = resultExcluded.Aggs
                    .Terms("agg_by_subcats_excluded")
                    .Buckets
                    .ToDictionary(k => k.Key, v => (int)v.DocCount);
            }

            var subCatsDisplayList = new List<SelectListItem>();

            if (subCatsDictionaryIncluded.Count() <= 0)
            {
                foreach (var item in subCatsDictionaryFields)
                {
                    var selectListItem = new SelectListItem
                    {
                        Text = item.Key + ", " + item.Value,
                        Value = item.Key,
                        Selected = false
                    };
                    subCatsDisplayList.Add(selectListItem);
                }
            }
            else
            {
                foreach (var item in subCatsDictionaryFields)
                {
                    var selectListItem = new SelectListItem
                    {
                        Text = item.Key + ", " + item.Value,
                        Value = item.Key,
                        Selected = true
                    };
                    subCatsDisplayList.Add(selectListItem);
                }

                foreach (var item in subCatsDictionaryExcluded)
                {
                    var selectListItem = new SelectListItem
                    {
                        Text = item.Key + ", " + item.Value,
                        Value = item.Key,
                        Selected = false
                    };
                    subCatsDisplayList.Add(selectListItem);
                }
            }

            //foreach (var item in catsDictionaryIncluded)
            //{
            //    var selectListItem = new SelectListItem { Text = item.Key + ", " + item.Value, Value = item.Key, Selected = true };
            //    subCatsDisplayList.Add(selectListItem);
            //}

            return subCatsDisplayList;
        }

        private List<SelectListItem> GetCategoryDisplayList(ISearchResponse<Post> result, ISearchResponse<Post> resultExcluded)
        {
            var catsDictionaryIncluded = new Dictionary<string, int>();
            if (result.Aggs.Terms("agg_by_cats_included") != null)
            {
                catsDictionaryIncluded = result.Aggs
                .Terms("agg_by_cats_included")
                .Buckets
                .ToDictionary(k => k.Key, v => (int)v.DocCount);
            }

            var catsDictionaryFields = new Dictionary<string, int>();
            if (result.Aggs.Terms("agg_by_cats_fields") != null)
            {
                catsDictionaryFields = result.Aggs
                    .Terms("agg_by_cats_fields")
                    .Buckets
                    .ToDictionary(k => k.Key, v => (int)v.DocCount);
            }

            var catsDictionaryExcluded = new Dictionary<string, int>();
            if (resultExcluded.Aggs.Terms("agg_by_cats_excluded") != null)
            {
                catsDictionaryExcluded = resultExcluded.Aggs
                    .Terms("agg_by_cats_excluded")
                    .Buckets
                    .ToDictionary(k => k.Key, v => (int)v.DocCount);
            }

            var catsDisplayList = new List<SelectListItem>();

            if (catsDictionaryIncluded.Count() <= 0)
            {
                foreach (var item in catsDictionaryFields)
                {
                    var selectListItem = new SelectListItem
                    {
                        Text = item.Key + ", " + item.Value,
                        Value = item.Key,
                        Selected = false
                    };
                    catsDisplayList.Add(selectListItem);
                }
            }
            else
            {
                foreach (var item in catsDictionaryFields)
                {
                    var selectListItem = new SelectListItem
                    {
                        Text = item.Key + ", " + item.Value,
                        Value = item.Key,
                        Selected = true
                    };
                    catsDisplayList.Add(selectListItem);
                }

                foreach (var item in catsDictionaryExcluded)
                {
                    var selectListItem = new SelectListItem
                    {
                        Text = item.Key + ", " + item.Value,
                        Value = item.Key,
                        Selected = false
                    };
                    catsDisplayList.Add(selectListItem);
                }
            }

            return catsDisplayList;
        }

        private ISearchResponse<Post> GetResultFromExcludedCatsAndSubCats(IEnumerable<string> userCats, IEnumerable<string> userSubCats)
        {
            var resultExcluded = _client.Search<Post>(s => s
                .Aggregations(aggs => aggs
                    .Terms("agg_by_cats_excluded", term => term
                        .Field(p => p.Category.Suffix("keyword"))
                        .Exclude(userCats)
                    )
                    .Terms("agg_by_subcats_excluded", term => term
                        .Field(p => p.SubCategory.Suffix("keyword"))
                        .Exclude(userSubCats)
                    )
                )
                .Size(10)
            );

            return resultExcluded;
        }

        private ISearchResponse<Post> GetResultFromIncludedCatsAndSubCats(IEnumerable<string> userCats, IEnumerable<string> userSubCats)
        {
            var result = _client.Search<Post>(s => s
                 .Query(q => q
                     .Bool(b => b
                         .Must(mu => mu
                            .Terms(c => c
                                .Name("user_cats_query")
                                .Boost(1.5)
                                .Field(p => p.Category)
                                .Terms(userCats)
                            ) && mu //Add two must statments together
                            .Terms(c => c
                                .Name("user_subcats_query")
                                .Boost(1.2)
                                .Field(p => p.SubCategory)
                                .Terms(userSubCats) // The solution!!??
                            )
                         )
                     )
                 )
                 .Aggregations(aggs => aggs
                    .Terms("agg_by_cats_included", term => term
                        .Field(p => p.Category.Suffix("keyword"))
                        .Include(userCats)
                    )
                    .Terms("agg_by_subcats_included", term => term
                        .Field(p => p.SubCategory.Suffix("keyword"))
                        .Include(userSubCats)
                    )
                    .Terms("agg_by_cats_fields", term => term
                        .Field(p => p.Category.Suffix("keyword"))
                    )
                    .Terms("agg_by_subcats_fields", term => term
                        .Field(p => p.SubCategory.Suffix("keyword"))
                    )
                )
                .Size(20)
            );

            return result;
        }

        [HttpGet]
        public IActionResult FacetSearchGet(string queryString, List<string> selectedCategories, List<string> selectedSubCategories)
        {
            //Read input from queryString variable (sent here by AJAX) and decode the string below
            //var data = queryString.Split(','); //etc.

            var model = new SearchViewModel();
            
            var userCats = selectedCategories;
            var userSubCats = selectedSubCategories;

            if (userSubCats.Count == 0)
            {
                userSubCats.Add("sub1");
                userSubCats.Add("sub2");
                userSubCats.Add("sub3");
            }

            if (userCats.Count == 0)
            {
                userCats.Add("test");
                userCats.Add("personal");
                userCats.Add("news");
            }

            var result = GetResultFromIncludedCatsAndSubCats(userCats, userSubCats);

            model.Posts = result.Documents;
            model.AmountOfHits = (int)result.Total;

            var resultExcluded = GetResultFromExcludedCatsAndSubCats(userCats, userSubCats);

            var catsDisplayList = GetCategoryDisplayList(result, resultExcluded);
            var subCatsDisplayList = GetSubCategoryDisplayList(result, resultExcluded);

            model.AvailableCategories = catsDisplayList;
            model.AvailableSubCategories = subCatsDisplayList;

            return View(model);
        }

        [HttpGet]
        public IActionResult FacetSearchQuery(string queryString, string selectedCategories, string selectedSubCategories)
        {
            //queryString skulle kunna vara en lista!
            bool queryIsValid = !string.IsNullOrWhiteSpace(queryString);

            var model = new SearchViewModel();
            ISearchResponse<Post> result1;
            //ISearchResponse<Post> result2;
            string[] catsList = { };
            string[] subCatsList = { "sub1", "sub2", "sub3" };

            if (queryIsValid)
            {
                string[] itemList = queryString.Split('=');

                if (itemList.Count() == 2)
                {
                    if (itemList[1] != null)
                    {
                        catsList = itemList[1].Split(',');
                    }
                }
                
                //if (itemList[1] != null)
                //{
                //    subCatsList = itemList[1].Split(',');
                //}
            }

            if (catsList.Count() > 0 || subCatsList.Count() > 0)
            {
                result1 = _client.Search<Post>(s => s
                    .Query(q => q
                         .Bool(b => b
                             .Must(mu => mu
                                .Terms(c => c
                                    .Name("user_cats_query")
                                    .Boost(1.5)
                                    .Field(p => p.Category)
                                    .Terms(catsList)
                                ) && mu //Add two must statments together
                                .Terms(c => c
                                    .Name("user_subcats_query")
                                    .Boost(1.2)
                                    .Field(p => p.SubCategory)
                                    .Terms(subCatsList) // The solution!!??
                                )
                             )
                         )
                     )
                    .Aggregations(aggs => aggs
                        .Terms("agg_by_cats_included", term => term
                            .Field(p => p.Category.Suffix("keyword"))
                            .Include(catsList)
                        )
                        .Terms("agg_by_subcats_included", term => term
                            .Field(p => p.SubCategory.Suffix("keyword"))
                            .Include(subCatsList)
                        )
                    )
                    .Size(20)
                );
            }
            else
            {
                result1 = _client.Search<Post>(s => s
                    .Aggregations(aggs => aggs
                        .Terms("agg_by_cats", term => term
                            .Field(p => p.Category.Suffix("keyword"))
                        )
                        .Terms("agg_by_subcats", term => term
                            .Field(p => p.SubCategory.Suffix("keyword"))
                        )
                    )
                    .Size(20)
                );
            }

            model.Posts = result1.Documents;
            model.AmountOfHits = (int)result1.Total;

            //var catsDictionary = result1.Aggs
            //    .Terms("agg_by_cats")
            //    .Buckets
            //    .ToDictionary(k => k.Key, v => v.DocCount);

            //var subCatsDictionary = result1.Aggs
            //    .Terms("agg_by_subcats")
            //    .Buckets
            //    .ToDictionary(k => k.Key, v => v.DocCount);

            var resultExcluded = _client.Search<Post>(s => s
                .Aggregations(aggs => aggs
                    .Terms("agg_by_cats_excluded", term => term
                        .Field(p => p.Category.Suffix("keyword"))
                        .Exclude(catsList)
                    )
                    .Terms("agg_by_subcats_excluded", term => term
                        .Field(p => p.SubCategory.Suffix("keyword"))
                        .Exclude(subCatsList)
                    )
                ).Size(10)
            );

            var catsDisplayList = GetCategoryDisplayList(result1, resultExcluded);
            var subCatsDisplayList = GetSubCategoryDisplayList(result1, resultExcluded);

            model.AvailableCategories = catsDisplayList;
            model.AvailableSubCategories = subCatsDisplayList;

            return PartialView("FacetSearch", model);
        }

        [HttpGet]
        public IActionResult FacetSearchQueryOld(string queryString, string selectedCategories, string selectedSubCategories)
        {
            //queryString skulle kunna vara en lista!
            bool queryIsValid = !string.IsNullOrWhiteSpace(queryString);

            ISearchResponse<Post> result1;
            //ISearchResponse<Post> result2;
            string[] catsList = { };
            string[] subCatsList = { };

            if (queryIsValid)
            {
                string[] itemList = queryString.Split('=');

                if (itemList.Count() == 2)
                {
                    if (itemList[1] != null)
                    {
                        catsList = itemList[1].Split(',');
                    }
                }


                //if (itemList[1] != null)
                //{
                //    subCatsList = itemList[1].Split(',');
                //}
            }

            if (catsList.Count() > 0 || subCatsList.Count() > 0)
            {
                result1 = _client.Search<Post>(s => s
                    .Query(q => q
                         .Bool(b => b
                             .Must(mu => mu
                                .Terms(c => c
                                    .Name("user_cats_query")
                                    .Boost(1.5)
                                    .Field(p => p.Category)
                                    .Terms(catsList)
                                ) && mu //Add two must statments together
                                .Terms(c => c
                                    .Name("user_subcats_query")
                                    .Boost(1.2)
                                    .Field(p => p.SubCategory)
                                    .Terms(subCatsList) // The solution!!??
                                )
                             )
                         )
                     )
                    .Aggregations(aggs => aggs
                        .Terms("agg_by_cats", term => term
                            .Field(p => p.Category.Suffix("keyword"))
                        //.Include(catsList)
                        )
                        .Terms("agg_by_subcats", term => term
                            .Field(p => p.SubCategory.Suffix("keyword"))
                        //.Include(subCatsList)
                        )
                    )
                    .Size(20)
                );

                //result2 = _client.Search<Post>(s => s
                //    .Aggregations(aggs => aggs
                //        .Terms("agg_by_subcats", term => term
                //            .Field(p => p.SubCategory.Suffix("keyword"))
                //            .Include(dataList)
                //        )
                //    )
                //    .Size(20)
                //);
            }
            else
            {
                result1 = _client.Search<Post>(s => s
                    .Aggregations(aggs => aggs
                        .Terms("agg_by_cats", term => term
                            .Field(p => p.Category.Suffix("keyword"))
                        )
                        .Terms("agg_by_subcats", term => term
                            .Field(p => p.SubCategory.Suffix("keyword"))
                        )
                    )
                    .Size(20)
                );

                //result2 = _client.Search<Post>(s => s
                //    .Aggregations(aggs => aggs
                //        .Terms("agg_by_subcats", term => term
                //            .Field(p => p.SubCategory.Suffix("keyword"))
                //        )
                //    )
                //    .Size(20)
                //);
            }

            var catsDictionary = result1.Aggs
                .Terms("agg_by_cats")
                .Buckets
                .ToDictionary(k => k.Key, v => v.DocCount);

            var subCatsDictionary = result1.Aggs
                .Terms("agg_by_subcats")
                .Buckets
                .ToDictionary(k => k.Key, v => v.DocCount);

            //var subCatsDictionary = result2.Aggs
            //    .Terms("agg_by_subcats")
            //    .Buckets
            //    .ToDictionary(k => k.Key, v => v.DocCount);

            string dictionaryJson1 = JsonConvert.SerializeObject(catsDictionary, Formatting.Indented);
            string dictionaryJson2 = JsonConvert.SerializeObject(subCatsDictionary, Formatting.Indented);
            string totalHitsJson = JsonConvert.SerializeObject(result1.Total, Formatting.Indented);
            string resultJson = JsonConvert.SerializeObject(result1.Documents, Formatting.Indented);

            return Ok($"Hits: {totalHitsJson} \n Aggs Categories: {dictionaryJson1} \n Aggs: SubCategories {dictionaryJson2} " +
                $" Result: {resultJson}");
        }

        private IEnumerable<SelectListItem> GetAllCategories()
        {
            var result = _client.Search<Post>(s => s
                .Aggregations(aggs => aggs
                    .Terms("agg_by_cats", term => term
                        .Field(p => p.Category.Suffix("keyword"))
                    )
                ).Size(10)
            );

            var catsDictionary = result.Aggs
                .Terms("agg_by_cats")
                .Buckets
                .ToDictionary(k => k.Key, v => v.DocCount);

            var catsDisplayList = new List<SelectListItem>();

            foreach (var item in catsDictionary)
            {
                var selectListItem = new SelectListItem { Text = item.Key + ", " + item.Value, Value = item.Key };
                catsDisplayList.Add(selectListItem);
            }

            return catsDisplayList;
        }

        private IEnumerable<SelectListItem> GetAllSubCategories()
        {
            var result = _client.Search<Post>(s => s
                .Aggregations(aggs => aggs
                    .Terms("agg_by_cats", term => term
                        .Field(p => p.SubCategory.Suffix("keyword"))
                    )
                ).Size(10)
            );

            var subCatsDictionary = result.Aggs
                .Terms("agg_by_cats")
                .Buckets
                .ToDictionary(k => k.Key, v => v.DocCount);

            var subCatsDisplayList = new List<SelectListItem>();

            foreach (var item in subCatsDictionary)
            {
                var selectListItem = new SelectListItem { Text = item.Key + ", " + item.Value, Value = item.Key };
                subCatsDisplayList.Add(selectListItem);
            }

            return subCatsDisplayList;
        }

        private IEnumerable<SelectListItem> GetTags()
        {
            var resultTags = _client.Search<Post>(s => s
                .Aggregations(aggs2 => aggs2
                    .Terms("agg_by_tags", term2 => term2
                        .Field(p => p.Tags.First())
                    )
                ).Size(10)
            );

            var tagsDictionary = resultTags.Aggs
                .Terms("agg_by_tags")
                .Buckets
                .ToDictionary(k => k.Key, v => v.DocCount);

            var tagsDisplayList = new List<SelectListItem>();

            foreach (var item in tagsDictionary)
            {
                var selectListItem = new SelectListItem { Text = item.Key + ", " + item.Value, Value = item.Key };
                tagsDisplayList.Add(selectListItem);
            }

            return tagsDisplayList;
        }

        private IEnumerable<SelectListItem> GetTagsWithIncluded(IEnumerable<string> tagsList)
        {
            var resultTags = _client.Search<Post>(s => s
                .Aggregations(aggs2 => aggs2
                    .Terms("agg_by_tags", term2 => term2
                        .Field(p => p.Tags.First())
                        .Include(tagsList)
                    )
                )
            );

            var resultTags2 = _client.Search<Post>(s => s
                .Aggregations(aggs2 => aggs2
                    .Terms("agg_by_tags", term2 => term2
                        .Field(p => p.Tags.First())
                        .Exclude(tagsList)
                    )
                )
            );

            var tagsDictionary = resultTags.Aggs
                .Terms("agg_by_tags")
                .Buckets
                .ToDictionary(k => k.Key, v => v.DocCount);

            var tagsDictionary2 = resultTags2.Aggs
                .Terms("agg_by_tags")
                .Buckets
                .ToDictionary(k => k.Key, v => v.DocCount);

            var tagsDisplayList = new List<SelectListItem>();

            foreach (var item in tagsDictionary)
            {
                var selectListItem = new SelectListItem { Text = item.Key + ", " + item.Value, Value = item.Key, Selected = true };
                tagsDisplayList.Add(selectListItem);
            }

            foreach (var item in tagsDictionary2)
            {
                var selectListItem = new SelectListItem { Text = item.Key + ", " + item.Value, Value = item.Key, Selected = false };
                tagsDisplayList.Add(selectListItem);
            }

            return tagsDisplayList;
        }

        private List<string> ConvertDictionaryToList(Dictionary<string, long> dictionary)
        {
            var list = new List<string>();
            foreach (var item in dictionary)
            {
                list.Add($"{item.Key},{item.Value}");
            }

            return list;
        }

        public IActionResult Filters()
        {

            var tagsList = new string[] { "ex", "tempor" };
            var categoryList = new string[] { "test", "personal", "news" };

            var resultAgg1 = _client.Search<Post>(s => s
                            .Aggregations(aggs => aggs
                                .Terms("agg_by_cats", term => term
                                    .Field(p => p.Category.First().Suffix("keyword")) //adds .keyword at the end
                                    .Include(categoryList)
                                )
                            )
                      );

            var resultAgg2 = _client.Search<Post>(s => s
                            .Aggregations(aggs2 => aggs2
                                .Terms("agg_by_tags", term2 => term2
                                    .Field(p => p.Tags.First())
                                    .Include(tagsList)
                                )
                            )
                      );

            var dictionaryCats = resultAgg1.Aggs.Terms("agg_by_cats").Buckets.ToDictionary(x => x.Key, y => y.DocCount.Value);
            var dictionaryTags = resultAgg2.Aggs.Terms("agg_by_tags").Buckets.ToDictionary(x => x.Key, y => y.DocCount.Value);

            var result = _client.Search<Post>(s => s
                    .Query(q => q
                        .Bool(bq => bq
                            .Filter(
                                fq => fq.Terms(t => t.Field(f => f.Tags).Terms(tagsList)),
                                fq => fq.Terms(t => t.Field(f => f.Category).Terms(categoryList))
                            )
                        )
                    )
                    .Size(200)
            );

            //Converts results to Json
            string totalHitsJson = JsonConvert.SerializeObject(resultAgg2.Total, Formatting.Indented);
            string documentsJson = JsonConvert.SerializeObject(resultAgg2.Documents, Formatting.Indented);
            string dictionaryJson1 = JsonConvert.SerializeObject(dictionaryCats, Formatting.Indented);
            string dictionaryJson2 = JsonConvert.SerializeObject(dictionaryTags, Formatting.Indented);

            return Ok($"Hits: {totalHitsJson} \n Aggs Categories: {dictionaryJson1} \n Aggs: Tags {dictionaryJson2} " +
                $"\n Documents: {documentsJson}");
        }

        [HttpGet]
        public IActionResult Suggestions(string queryString)
        {

            var result = _client.Search<Post>(s => s
                //.Source(sf => sf
                //    .Includes(f => f
                //        .Field(ff => ff.Title.Suffix("keyword"))
                //        //.Field(ff => ff.Category)
                //        //.Field(ff => ff.Tags)
                //    )
                //)
                .Suggest(su => su
                    .Term("my-term-suggest", t => t
                        .Field(p => p.Title)
                        .Text(queryString)
                    )
                    .Completion("post-suggestions", c => c
                        .Field(p => p.Suggest)
                        .Prefix(queryString)
                        
                    )
                )
            );
            //Det är tomt?!

            var suggestions = result.Suggest["post-suggestions"]
                .FirstOrDefault()
                .Options
                .Select(suggest => new
                {
                    title = suggest.Source.Title,
                    //category = suggest.Source.Category,
                    //tags = suggest.Source.Tags
                });

            string suggestedJson = JsonConvert.SerializeObject(suggestions, Formatting.Indented);

            return Ok($"Suggestion List: {suggestedJson}");
        }

        public IActionResult SeedData()
        {
            if (_client.IndexExists("my_blog").Exists)
            {
                return Ok("The Index 'my_blog' already exists.");
            }

            //Mapping required? - Yes for Attributes on Models to work
            var indexDescriptor = new CreateIndexDescriptor("my_blog")
                    .Mappings(ms => ms
                        .Map<Post>(m => m.AutoMap()
                        
                        ));

            _client.CreateIndex("my_blog", i => indexDescriptor);

            //IndexCustomBlogs();

            //Adds test data from a json text file
            var convertor = new JsonDataConvertor();
            var listWithPostsRoots = convertor.JsonToPostList();

            foreach (var postRoot in listWithPostsRoots)
            {
                var post = new Post()
                {
                    UserId = postRoot.userId,
                    Title = postRoot.title,
                    Tags = postRoot.tags,
                    Category = postRoot.category,
                    SubCategory = postRoot.subCategory,
                    PostDate = Convert.ToDateTime(postRoot.postDate),
                    PostText = postRoot.postText
                };

                //var result = _client.Index(post, idx => idx.Index("my_blog"));

                var result = _client.Bulk(b => b
                    .Index<Post>(i => i.Document(post))
                );

                if (!result.IsValid)
                {
                    return Ok(result.DebugInformation);
                }
            }

            if (_client.IndexExists("my_blog").Exists)
            {
                return Ok($"Blog Posts has been sucessfully created.");
            }
            else
            {
                return NotFound($"Something went wrong.");
            }

        }

        // Index custom blogs post if needed
        private void IndexCustomBlogs()
        {
            var newBlogPost1 = new Post
            {
                UserId = 1,
                Title = "My First Blog Post",
                Tags = new List<string>() { "blog", "tag1", "today" },
                Category = "test",
                PostDate = DateTime.Now,
                PostText = "This is my first blog post from NEST."
            };

            var newBlogPost2 = new Post
            {
                UserId = 2,
                Title = "Another Blog with Secret",
                Tags = new List<string>() { "blog", "tag2", "future" },
                Category = "personal",
                PostDate = DateTime.Now.AddDays(1),
                PostText = "This is another blog post from NEST, with a secret message."
            };

            var newBlogPost3 = new Post
            {
                UserId = 3,
                Title = "My Third Blog Post",
                Tags = new List<string>() { "blog", "tag3", "past" },
                Category = "test",
                PostDate = DateTime.Now.AddDays(-5),
                PostText = "This is a third blog post from NEST."
            };

            var newBlogPost4 = new Post
            {
                UserId = 4,
                Title = "The Future Blog",
                Tags = new List<string>() { "blog", "tag4", "future" },
                Category = "news",
                PostDate = DateTime.Now.AddDays(5),
                PostText = "This is a blog post form the future, Such wow."
            };

            var newBlogPost5 = new Post
            {
                UserId = 5,
                Title = "The Old Blog Post",
                Tags = new List<string>() { "blog", "tag5", "past" },
                Category = "news",
                PostDate = DateTime.Now.AddDays(-10),
                PostText = "This is a blog post form the 10 days ago, really outdated..."
            };

            _client.Index(newBlogPost1, idx => idx.Index("my_blog"));
            _client.Index(newBlogPost2, idx => idx.Index("my_blog"));
            _client.Index(newBlogPost3, idx => idx.Index("my_blog"));
            _client.Index(newBlogPost4, idx => idx.Index("my_blog"));
            _client.Index(newBlogPost5, idx => idx.Index("my_blog"));
        }

        public IActionResult DeleteData()
        {
            if (_client.IndexExists("my_blog").Exists)
            {
                var result = _client.DeleteIndex("my_blog");
                if (!result.IsValid)
                {
                    return Ok(result.DebugInformation);
                }
                return Ok("Some data has been deleted");
            }
            else
            {
                return NotFound("Something went wrong...");
            }
        }

    }
}