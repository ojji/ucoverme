using McMaster.Extensions.CommandLineUtils;
using UCoverme.Commands;
using UCoverme.Utils;

namespace UCoverme
{
    static class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "ucoverme"
            };
            app.HelpOption("-?|--help|-h");

            app.AddCommand(UCovermeCommand.InstrumentCommand());
            app.AddCommand(UCovermeCommand.UninstrumentCommand());
            app.AddCommand(UCovermeCommand.CreateReportCommand());

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });

            app.Execute(args);
        }
    }
}
