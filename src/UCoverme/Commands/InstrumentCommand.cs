using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using UCoverme.DataCollector.Utils;
using UCoverme.Model;
using UCoverme.ModelBuilder;
using UCoverme.ModelBuilder.Filters;
using UCoverme.Options;
using UCoverme.Utils;

namespace UCoverme.Commands
{
    public class InstrumentCommand : UCovermeCommand
    {
        public override string Name => "instrument";
        public override string Description => "Sets up instrumentation on a target assembly.";

        private readonly PathOption _targetOption;
        private readonly CommandOption _disableDefaultFilters;
        private readonly FilterOption _filterOption;

        private readonly CommandOption _dumpMethodsOption;
        
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

            _filterOption = new FilterOption("--filter")
            {
                Description =
                    "A list of filters to apply to selectively include or exclude assemblies or the contained classes from the coverage results.",
                ShowInHelpText = true
            };

            _dumpMethodsOption = new CommandOption("--dump", CommandOptionType.NoValue);
            Options.Add(_dumpMethodsOption);
        
            Options.Add(_targetOption);
            Options.Add(_disableDefaultFilters);
            Options.Add(_filterOption);
        }

        public override void Execute()
        {
            var targetPaths = _targetOption.ParsedValue.ToArray();

            if (targetPaths.Length == 0)
            {
                throw new ArgumentException("No target assembly matched the specified pattern.");
            }

            List<IFilter> filters = new List<IFilter>();
            if (_filterOption.HasValue())
            {
                filters.AddRange(_filterOption.ParsedValue);
            }

            if (!_disableDefaultFilters.HasValue())
            {
                filters.AddRange(AssemblyFilter.GetDefaultFilters());
            }

            var coverageDirectory = GetCoverageDirectory();

            if (!Directory.Exists(coverageDirectory))
            {
                Directory.CreateDirectory(coverageDirectory);
            }

            var projectId = Guid.NewGuid();
            var projectPath = Path.Combine(coverageDirectory, $"{projectId}.ucovermeproj");

            UCovermeProject uCovermeProject = new UCovermeProject(projectId, projectPath);

            foreach (var path in targetPaths)
            {
                var model = AssemblyBuilder.Build(path, filters);
                uCovermeProject.AddAssembly(model);
            }

            uCovermeProject.Instrument();
            uCovermeProject.WriteToFile();

            if (_dumpMethodsOption.HasValue())
            {
                var logFile = Path.Combine(coverageDirectory, "methods.log");
                logFile.Empty();
                foreach (var method in uCovermeProject.Assemblies.Where(a => !a.IsSkipped).SelectMany(a => a.Classes).SelectMany(c => c.Methods))
                {
                    logFile.Log(method.Debug());
                }
            }
        }
    }
}