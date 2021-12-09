using Microsoft.Extensions.Configuration;
using RPCutover.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPCutover.Handlers
{
    public class ConvertCommandHandler
    {
        public static async Task<int> Handle(IConfiguration config, string folderPath)
        {
            string dnFilePath = "";
            string tkFilePath = "";

            foreach (var item in Directory.EnumerateFiles(folderPath).Where(x => Path.GetFileName(x).StartsWith("SiteStockexport_")))
            {
                if (item.Contains("DN"))
                    dnFilePath = item;
                if (item.Contains("TK"))
                    tkFilePath = item;
            }

            string[] newFile = AddLineBreakToFirstLine(dnFilePath);
            await File.WriteAllLinesAsync(dnFilePath, newFile);

            newFile = AddLineBreakToFirstLine(tkFilePath);
            await File.WriteAllLinesAsync(tkFilePath, newFile);

            // Re-open DN as CSV
            string[] dnFile = await File.ReadAllLinesAsync(dnFilePath);

            List<StockExportModel> dnExport = dnFile
                .Skip(1)
                .Select(x => x.Split(','))
                .Select(x => new StockExportModel(x))
                .ToList();

            // Re-open TK as CSV
            string[] tkFile = await File.ReadAllLinesAsync(tkFilePath);

            List<StockExportModel> tkExport = tkFile
                .Skip(1)
                .Select(x => x.Split(','))
                .Select(x => new StockExportModel(x))
                .ToList();

            //  Replace the On_Hand_Quantity's with those of the DN file (lookup vs Product_External_ID)
            foreach (var item in tkExport)
            {
                if (item.Product_External_ID == "NULL")
                {
                    item.On_Hand_Quantity = "0";
                }
                else
                {
                    var match = dnExport.Where(x => x.Product_External_ID == item.Product_External_ID);
                    if (match.Count() == 0)
                        item.On_Hand_Quantity = "0";
                    else
                        item.On_Hand_Quantity = match.First().On_Hand_Quantity;
                }
            }

            // Create new CSV
            var yest = DateTime.Now.AddDays(-1);
            var yesterday = yest.ToString("yyyy-MM-dd");

            List<string> csvOutput = new() { "Site_Internal_ID,Worksheet_ID,Product_Internal_ID,On_Hand_Quantity,Count_Date" };
            foreach (var item in tkExport)
            {
                csvOutput.Add($"{item.Site_Internal_ID},1068175,{item.Product_Internal_ID},{item.On_Hand_Quantity},{yesterday} 23:59:59");
            }

            await File.WriteAllLinesAsync(@"C:\Users\GaryM\source\repos\RPCutover\Test\Stock_Upload.csv", csvOutput.ToArray());

            return 0;
        }

        private static string[] AddLineBreakToFirstLine(string filePath)
        {
            List<string> lines = File.ReadAllLines(filePath).ToList();

            //      Find end of On_Hand_Quantity and insert line break
            var x = lines[0].IndexOf("On_Hand_Quantity") + 16;
            string line = lines[0].Substring(x);

            if (!string.IsNullOrEmpty(line))
            {
                lines[0] = lines[0].Substring(0, x);
                lines.Insert(1, line);
            }  

            return lines.ToArray();
        }
    }  
}
