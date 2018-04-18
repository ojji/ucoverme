using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
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
        private readonly CommandOption _coverageDirectoryOption;
        private readonly CommandOption _disableDefaultFilters;
        private readonly FilterOption _filterOption;

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

            _coverageDirectoryOption = new CommandOption("--coverage-directory", CommandOptionType.SingleValue)
            {
                Description = "Sets the directory where the coverage files and the necessary artifacts are created.",
                ShowInHelpText = true
            };

            Options.Add(_targetOption);
            Options.Add(_disableDefaultFilters);
            Options.Add(_filterOption);
            Options.Add(_coverageDirectoryOption);
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

            UCovermeProject uCovermeProject = new UCovermeProject(projectId);

            foreach (var path in targetPaths)
            {
                var model = InstrumentedAssemblyBuilder.Build(path, filters);
                uCovermeProject.AddAssembly(model);
            }

            uCovermeProject.Instrument();

            var jsonSerializer = new JsonSerializer();
            using (var sw = new StreamWriter(File.Create(projectPath)))
            using (var jsonWriter = new JsonTextWriter(sw))
            {
                jsonSerializer.Serialize(jsonWriter, uCovermeProject);
            }

            Console.WriteLine($"Project file created: {projectPath}");
        }

        private string GetCoverageDirectory()
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