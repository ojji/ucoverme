using System;
using System.Collections.Generic;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using UCoverme.Model;

namespace UCoverme.Commands
{
    public class UninstrumentCommand : UCovermeCommand
    {
        public override string Name => "uninstrument";
        public override string Description => "Restores the instrumented assemblies to their original version.";

        private readonly CommandOption _coverageDirectoryOption;

        public UninstrumentCommand()
        {
            _coverageDirectoryOption = new CommandOption("--coverage-directory", CommandOptionType.SingleValue)
            {
                Description = "Sets the directory where the coverage files and the necessary artifacts were created.",
                ShowInHelpText = true
            };

            Options.Add(_coverageDirectoryOption);
        }

        public override void Execute()
        {
            var coverageDirectory = GetCoverageDirectory();

            if (!Directory.Exists(coverageDirectory))
            {
                throw new ArgumentException($"The coverage directory does not exist");
            }

            var projectFiles = Directory.GetFileSystemEntries(coverageDirectory, "*.ucovermeproj");

            if (projectFiles.Length == 0)
            {
                throw new ArgumentException($"Could not find valid project files in the coverage directory.");
            }

            var projects = new List<UCovermeProject>();
            var jsonSerializer = new JsonSerializer();
            foreach (var projectFile in projectFiles)
            {
                using (var sr = new StreamReader(File.OpenRead(projectFile)))
                using (var jsonReader = new JsonTextReader(sr))
                {
                    var project = jsonSerializer.Deserialize<UCovermeProject>(jsonReader);
                    projects.Add(project);
                }
            }

            foreach (var project in projects)
            {
                project.Uninstrument();
            }
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