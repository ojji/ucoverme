using System;
using McMaster.Extensions.CommandLineUtils;
using UCoverme.Commands;

namespace UCoverme.Utils
{
    public static class CommandLineApplicationExtensions
    {
        public static void AddCommand(this CommandLineApplication app, UCovermeCommand command)
        {
            app.Command(command.Name, c =>
            {
                c.Description = command.Description;
                c.HelpOption("-?|--help|-h");
                c.Arguments.AddRange(command.Arguments);
                c.Options.AddRange(command.Options);
                c.OnExecute(() =>
                {
                    try
                    {
                        command.Execute();
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine(e.Message);
                        Console.ResetColor();
                        c.ShowHelp();
                        return 1;
                    }
                    return 0;
                });
            });
        }
    }
}