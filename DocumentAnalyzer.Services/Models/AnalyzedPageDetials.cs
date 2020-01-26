using System.Collections.Generic;

namespace Document.Analyzer.Services.Models
{
    public class AnalyzedPageDetials
    {
        public int? PageNumber { get; set; }
        public int NumberOfTables { get; set; }
        public List<AnalyzedTableDetails>? Tables { get; set; }
    }
}
