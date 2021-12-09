using Microsoft.Extensions.Configuration;
using FluentFTP;
using System.Diagnostics;

namespace RPCutover.Handlers
{
    public static class GetCommandHandler
    {
        public static async Task<int> Handle(IConfiguration config, string buid, string stationName)
        {
            // Pull values from config
            var host = config.GetValue<string>("FtpCredentials:Host");
            var remotePath = config.GetValue<string>("FtpCredentials:RemotePath");
            var tkUsername = config.GetValue<string>("FtpCredentials:TK:Username");
            var tkPassword = config.GetValue<string>("FtpCredentials:TK:Password");
            var dnUsername = config.GetValue<string>("FtpCredentials:DN:Username");
            var dnPassword = config.GetValue<string>("FtpCredentials:DN:Password");

            var folder = DateTime.Today.ToString("yyyy MM dd");
            var localDir = config.GetValue<string>("StoragePath") + @$"\{folder}\{stationName}";

            string dnFileName = $"SiteStockExport_DN_100{buid}.csv";
            string tkFileName = $"SiteStockExport_TK_100{buid}_#yyyyMMddhhmmss#.csv";

            bool completeSuccess = true;

            // Connect to sFTP server on DN login
            using FtpClient dnClient = new(host, dnUsername, dnPassword);
            await dnClient.AutoConnectAsync();

            // Collect file from buid
            await dnClient.SetWorkingDirectoryAsync(remotePath);
            try
            {
                await dnClient.DownloadFileAsync($@"{localDir}\{dnFileName}", $"{remotePath}/{dnFileName}");
            }
                
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                completeSuccess = false;
            }

            await dnClient.DisconnectAsync();

            // Connect to sFTP server on TK login
            using FtpClient tkClient = new(host, tkUsername, tkPassword);
            await tkClient.AutoConnectAsync();
            await tkClient.SetWorkingDirectoryAsync(remotePath);
            try
            {
                await tkClient.DownloadFileAsync($@"{localDir}\{tkFileName}", $"{remotePath}/{tkFileName}");
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                completeSuccess = false;
            }
            await tkClient.DisconnectAsync();

            if (completeSuccess)
            {
                ProcessStartInfo startInfo = new()
                {
                    Arguments = localDir,
                    FileName = "explorer.exe"
                };

                Process.Start(startInfo);
            }

            return 0;
        }
    }
}
