using Microsoft.Azure.CognitiveServices.FormRecognizer.Models;
using OfficeOpenXml;
using System.IO;
using System.Linq;

namespace Document.Analyzer.Services.Services
{
    public class FileBuilder : IFileBuilder
    {
        public Stream BuildFileFromAnalyzeResult(AnalyzeResult analyzeResult)
        {
            using var package = new ExcelPackage();
            foreach (var page in analyzeResult.Pages)
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets.Add(page.Number?.ToString());

                var rowIndex = 1;
                var rowIndexForColStart = 1;
                foreach (var table in page.Tables)
                {
                    foreach (var column in table.Columns)
                    {
                        rowIndexForColStart = rowIndex;

                        var columnIndex = table.Columns.IndexOf(column) + 1;
                        workSheet.SetValue(rowIndexForColStart, columnIndex, string.Join(" ", column.Header.Select(x => x.Text)));

                        rowIndexForColStart++;
                        foreach (var row in column.Entries)
                        {
                            workSheet.SetValue(rowIndexForColStart, columnIndex, row.Select(x => x.Text).Single());
                            rowIndexForColStart++;
                        }
                    }

                    rowIndex = rowIndexForColStart + 2;
                }
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            //stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
    }
}
