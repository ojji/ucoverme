using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;

namespace UCoverme.Commands
{
    public abstract class UCovermeCommand 
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public List<CommandOption> Options { get; }
        public List<CommandArgument> Arguments { get; }

        protected UCovermeCommand()
        {
            Arguments = new List<CommandArgument>();
            Options = new List<CommandOption>();
        }

        public abstract void Execute();

        public static UCovermeCommand BuildModelCommand()
        {
            return new BuildModelCommand();
        }

        public static UCovermeCommand InstrumentCommand()
        {
            return new InstrumentCommand();
        }
    }

    
}