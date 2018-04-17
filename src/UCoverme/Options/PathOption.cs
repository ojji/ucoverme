using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Glob;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace UCoverme.Options
{
    public class PathOption : CommandOption<string[]>
    {
        public PathOption(string template) : base(PathOptionValueParser.Singleton, template, CommandOptionType.SingleValue)
        {   
        }

        private class PathOptionValueParser : IValueParser<string[]>
        {
            public Type TargetType => typeof(string[]);
            public static PathOptionValueParser Singleton { get; } = new PathOptionValueParser();

            object IValueParser.Parse(string argName, string value, CultureInfo culture) => Parse(argName, value, culture);

            public string[] Parse(string argName, string value, CultureInfo culture)
            {
                List<string> assemblyPaths = new List<string>();
                string[] globPaths = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var globPath in globPaths)
                {
                    var di = new DirectoryInfo(Directory.GetCurrentDirectory());
                    assemblyPaths.AddRange(di.GlobFiles(globPath).Select(f => f.FullName));
                }

                return assemblyPaths.ToArray();
            }
        }
    }
}