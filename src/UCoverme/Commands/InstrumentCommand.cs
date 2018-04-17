using System;
using System.Collections.Generic;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using UCoverme.Model;
using UCoverme.ModelBuilder;
using UCoverme.ModelBuilder.Filters;
using UCoverme.Options;
using UCoverme.Utils;

namespace UCoverme.Commands
{
    public class InstrumentCommand : UCovermeCommand
    {
        private readonly PathOption _targetOption;
        private readonly CommandOption _disableDefaultFilters;
        private readonly FilterOption _filterOption;
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

            _filterOption = new FilterOption("--filter")
            {
                Description =
                    "A list of filters to apply to selectively include or exclude assemblies or the contained classes from the coverage results.",
                ShowInHelpText = true
            };

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

            UCovermeProject uCovermeProject = new UCovermeProject();

            foreach (var path in targetPaths)
            {
                var model = InstrumentedAssemblyBuilder.Build(path, filters);
                uCovermeProject.AddAssembly(model);
            }

            uCovermeProject.Instrument();
        }
    }
}