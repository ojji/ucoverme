using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using UCoverme.ModelBuilder.Filters;

namespace UCoverme.Options
{
    public class FilterOption : CommandOption<List<IFilter>>
    {
        public FilterOption(string template) : base(FilterOptionParser.SingletonInstance, template, CommandOptionType.SingleValue)
        {
        }

        private class FilterOptionParser : IValueParser<List<IFilter>>
        {
            public static readonly FilterOptionParser SingletonInstance = new FilterOptionParser();

            public Type TargetType => typeof(List<IFilter>);

            object IValueParser.Parse(string argName, string value, CultureInfo culture)
            {
                return Parse(argName, value, culture);
            }

            public List<IFilter> Parse(string argName, string value, CultureInfo culture)
            {
                var filters = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
                return filters.Select(AssemblyFilter.Parse).ToList();
            }
        }
    }
}