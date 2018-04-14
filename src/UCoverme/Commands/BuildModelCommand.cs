using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using UCoverme.ModelBuilder;
using UCoverme.Utils;

namespace UCoverme.Commands
{
    public class BuildModelCommand : UCovermeCommand
    {
        private string _logFile = Path.Combine(Directory.GetCurrentDirectory(), @"methods.log");
        private readonly CommandOption _pathOption;

        public override string Name => "buildmodel";
        public override string Description => "Builds an instrumentation model from the specified assembly.";

        public BuildModelCommand()
        {
            _pathOption = new CommandOption("--path|-p <PATH>", CommandOptionType.SingleValue)
            {
                Description = "Path of the assembly the model is build from",
                ShowInHelpText = true
            }.IsRequired(false, "The assembly path must be specified.");

            Options.Add(_pathOption);
        }

        public override void Execute()
        {
            Console.WriteLine($"Building model from {_pathOption.Value()}");

            /*var assemblyModel = AssemblyModelBuilder.Build(_pathOption.Value());

            using (var writer = new StreamWriter(File.Open(_logFile, FileMode.Create)))
            {
                foreach (var method in assemblyModel.Classes.SelectMany(c => c.Methods))
                {
                    writer.WriteLine(method.Debug());
                }
            }*/
        }
    }
}