using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using UCoverme.Instrumentation;
using UCoverme.ModelBuilder;
using UCoverme.Options;
using UCoverme.Utils;

namespace UCoverme.Commands
{
    public class InstrumentCommand : UCovermeCommand
    {
        private readonly PathOption _targetOption;
        private readonly CommandOption _disableDefaultFilters;
        public override string Name => "instrument";
        public override string Description => "Sets up instrumentation on a target assembly.";

        public InstrumentCommand()
        {
            _targetOption = new PathOption("--target|-t <PATH>")
            {
                Description = "The path of the target assemblies.",
                ShowInHelpText = true
            }.IsRequired(false, "The target assemblies path must be specified.");

            _disableDefaultFilters = new CommandOption("--disable-default-filters", CommandOptionType.NoValue)
            {
                Description = "Disables the default filters",
                ShowInHelpText = true
            };
            Options.Add(_targetOption);
            Options.Add(_disableDefaultFilters);
        }

        public override void Execute()
        {
            var targetPaths = _targetOption.ParsedValue.ToArray();

            if (targetPaths.Length == 0)
            {
                throw new ArgumentException("No target assembly matched the specified pattern.");
            }

            var assemblyModels =
                targetPaths.Select(path => AssemblyModelBuilder.Build(path, _disableDefaultFilters.HasValue())).ToList();

            Console.WriteLine("Parsed assemblies:");
            foreach (var model in assemblyModels)
            {
                var logFile = $"{model.AssemblyName.Split(',').First()}-{model.AssemblyPaths.OriginalAssemblyPath.GetHashCode()}.methods.log";
                using (var writer = new StreamWriter(File.Open(logFile, FileMode.Create)))
                {
                    foreach (var method in model.Classes.SelectMany(c => c.Methods))
                    {
                        writer.WriteLine(method.Debug());
                    }
                }

                if (model.IsSkipped)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"[DISABLED - {model.SkipReason.ToString()}] ");
                    Console.ResetColor();
                }
                else
                {
                    using (var instrumenter = new Instrumenter(model))
                    {
                        instrumenter.Instrument();
                    }
                }

                Console.WriteLine($"{model.AssemblyName} - {model.AssemblyPaths.OriginalAssemblyPath}");
            }
        }
    }
}