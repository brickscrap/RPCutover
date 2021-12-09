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
            var convertCommand = new Command("convert", "Shift the stock data from the DN file into the TK file, and generate a file ready for upload back to RP.")
            {
                new Argument<string>("folder-path", "Path to the site folder previously created using the get command.")
            };

            convertCommand.Handler = CommandHandler.Create<string>(async (folderPath) =>
            {
                await ConvertCommandHandler.Handle(_config, folderPath);
            });

            var rootCommand = new RootCommand
            {
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
