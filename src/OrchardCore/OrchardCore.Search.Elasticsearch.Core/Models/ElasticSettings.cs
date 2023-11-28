using System;
using OrchardCore.Contents.Indexing;

namespace OrchardCore.Search.Elasticsearch.Core.Models
{
    public class ElasticSettings
    {
        public static readonly string[] FullTextField = new string[] { IndexingConstants.FullTextKey };

        [Obsolete("This property will be removed in future releases.")]
        public const string StandardAnalyzer = "standardanalyzer";

        public string SearchIndex { get; set; }

        public string[] DefaultSearchFields { get; set; } = FullTextField;

        public bool AllowElasticQueryStringQueryInSearch { get; set; } = false;
    }
}
