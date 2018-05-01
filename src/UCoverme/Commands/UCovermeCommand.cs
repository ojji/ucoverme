using System.Collections.Generic;
using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace UCoverme.Commands
{
    public abstract class UCovermeCommand 
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public List<CommandOption> Options { get; }
        public List<CommandArgument> Arguments { get; }

        private readonly CommandOption _coverageDirectoryOption;

        protected UCovermeCommand()
        {
            Arguments = new List<CommandArgument>();
            Options = new List<CommandOption>();

            _coverageDirectoryOption = new CommandOption("--coverage-directory", CommandOptionType.SingleValue)
            {
                Description = "Sets the directory where the coverage files and the necessary artifacts are created.",
                ShowInHelpText = true
            };

            Options.Add(_coverageDirectoryOption);
        }

        public abstract void Execute();

        public static UCovermeCommand InstrumentCommand()
        {
            return new InstrumentCommand();
        }

        public static UCovermeCommand UninstrumentCommand()
        {
            return new UninstrumentCommand();
        }

        public static UCovermeCommand CreateReportCommand()
        {
            return new CreateReportCommand();
        }

        protected string GetCoverageDirectory()
        {
            if (!_coverageDirectoryOption.HasValue())
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "coverage");
            }

            var pathValue = _coverageDirectoryOption.Value();
            if (!Path.IsPathRooted(pathValue))
            {
                pathValue = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    pathValue);
            }

            return pathValue;
        }
    }

    
}