using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPCutover.Models
{
    public class StockExportModel
    {
        public string Site_Code { get; set; }
        public string Site_Internal_ID { get; set; }
        public string Product_Internal_ID { get; set; }
        public string Product_External_ID { get; set; }
        public string Product_Name { get; set; }
        public string Base_UOM { get; set; }
        public string On_Hand_Quantity { get; set; }

        public StockExportModel(string[] csvLine)
        {
            if (csvLine.Length != 7)
            {
                Console.WriteLine($"CSV file in an incorrect format, only {csvLine.Length} columns could be found (expecting 7)");
                Console.WriteLine($"Line: {csvLine}");
            }
                
            
            Site_Code = csvLine[0].Trim();
            Site_Internal_ID = csvLine[1].Trim();
            Product_Internal_ID = csvLine[2].Trim();
            if (string.IsNullOrWhiteSpace(csvLine[3]))
                Product_External_ID = "NULL";
            else
                Product_External_ID = csvLine[3].Trim();
            Product_Name = csvLine[4].Trim();
            Base_UOM = csvLine[5].Trim();

            if (int.Parse(csvLine[6]) < 0)
                On_Hand_Quantity = "0";
            else
                On_Hand_Quantity = csvLine[6].Trim();
        }
    }
}
