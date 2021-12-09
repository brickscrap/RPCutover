using Microsoft.Extensions.Configuration;
using RPCutover.Handlers;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace RPCutover
{
    public class AppService
    {
        private readonly IConfiguration _config;

        public AppService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<int> Run(string[] args)
        {
            var getCommand = new Command("get", "Collect the files from the RP sFTP server for both environvments (once they have been exported).")
            {
                new Argument<string>("internal-buid", "The Red Prairie internal business unit ID. Used to find and pull the stock export files from the sFTP server."),
                new Argument<string>("station-name", "Name of the station. Used to generate a folder to save the files to."),
            };

            getCommand.Handler = CommandHandler.Create<string, string>(async (buid, stationName) => 
            { 
                await GetCommandHandler.Handle(_config, buid, stationName);
            });

            var convertCommand = new Command("convert", "Shift the stock data from the DN file into the TK file, and generate a file ready for upload back to RP.")
            {
                new Argument<string>("folder-path", "Path to the site folder previously created using the get command.")
            };

            convertCommand.Handler = CommandHandler.Create<string>(async (folderPath) =>
            {
                await ConvertCommandHandler.Handle(_config, folderPath);
            });

            var uploadCommand = new Command("upload", "Upload the newly created stock file back to the RedPrairie sFTP server.")
            {
                new Argument<string>("file-path", "Path to the file created during the 'convert' command.")
            };

            var rootCommand = new RootCommand
            {
                getCommand,
                convertCommand
            };

            rootCommand.Name = "RpCutover";
            rootCommand.Description = "Tools to aid in cutover process from DN to TK Red Prairie";

            if (args.Length == 0)
            {
                var newArgs = new string[] { "-?" };
                args = newArgs;
            }

            return await rootCommand.InvokeAsync(args);
        }
    }
}
