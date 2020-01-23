using System;
using System.Collections.Generic;

namespace Document.Analyzer.Services.Models
{
    public class AnalyzedTableDetails
    {
        public List<Tuple<string, int>>? ColumnRowCountPair { get; set; }
    }
}
