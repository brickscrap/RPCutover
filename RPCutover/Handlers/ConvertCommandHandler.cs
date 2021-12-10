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

            if (!Directory.Exists(folderPath))
                return 1;

            foreach (var item in Directory.EnumerateFiles(folderPath).Where(x => Path.GetFileName(x).StartsWith("SiteStockexport_")))
            {
                if (item.Contains("DN"))
                    dnFilePath = item;
                if (item.Contains("TK"))
                    tkFilePath = item;
            }

            if (string.IsNullOrEmpty(dnFilePath) || string.IsNullOrEmpty(tkFilePath))
                return 1;

            Console.WriteLine($"Adding linebreak to DN file: {Path.GetFileName(dnFilePath)}");
            string[] newFile = AddLineBreakToFirstLine(dnFilePath);
            await File.WriteAllLinesAsync(dnFilePath, newFile);

            Console.WriteLine($"Adding linebreak to TK file: {Path.GetFileName(tkFilePath)}");
            newFile = AddLineBreakToFirstLine(tkFilePath);
            await File.WriteAllLinesAsync(tkFilePath, newFile);

            // Re-open DN as CSV
            Console.WriteLine($"Reading DN file.");
            string[] dnFile = await File.ReadAllLinesAsync(dnFilePath);

            List<StockExportModel> dnExport = dnFile
                .Skip(1)
                .Select(x => x.Split(','))
                .Select(x => new StockExportModel(x))
                .ToList();

            Console.WriteLine($"DN file read, {dnExport.Count} entries found.");

            // Re-open TK as CSV
            Console.WriteLine($"Reading TK file.");
            string[] tkFile = await File.ReadAllLinesAsync(tkFilePath);

            List<StockExportModel> tkExport = tkFile
                .Skip(1)
                .Select(x => x.Split(','))
                .Select(x => new StockExportModel(x))
                .ToList();

            Console.WriteLine($"TK file read, {tkExport.Count} entries found.");

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

            Console.WriteLine("Creating Stock_Upload.csv");
            List<string> csvOutput = new() { "Site_Internal_ID,Worksheet_ID,Product_Internal_ID,On_Hand_Quantity,Count_Date" };
            foreach (var item in tkExport)
            {
                csvOutput.Add($"{item.Site_Internal_ID},1068175,{item.Product_Internal_ID},{item.On_Hand_Quantity},{yesterday} 23:59:59");
            }

            try
            {
                await File.WriteAllLinesAsync(@$"{folderPath}\Stock_Import.csv", csvOutput.ToArray());
                Console.WriteLine($"Stock_Upload.csv generated in {folderPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Stock_Import.csv");
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }

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
