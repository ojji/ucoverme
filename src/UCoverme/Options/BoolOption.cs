using System;
using System.Globalization;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace UCoverme.Options
{
    public class BoolOption : CommandOption<bool>
    {
        public BoolOption(string template, CommandOptionType optionType) : base(BoolOptionValueParser.Singleton, template, optionType)
        {
        }

        private class BoolOptionValueParser : IValueParser<bool>
        {
            public Type TargetType => typeof(bool);
            public static BoolOptionValueParser Singleton { get; } = new BoolOptionValueParser();

            object IValueParser.Parse(string argName, string value, CultureInfo culture) => Parse(argName, value, culture);

            public bool Parse(string argName, string value, CultureInfo culture)
            {
                if (value == null) return default(bool);

                if (!bool.TryParse(value, out var result))
                {
                    if (short.TryParse(value, out var bit))
                    {
                        return bit != 0;
                    }

                    throw new FormatException($"Invalid value specified for {argName}. Cannot convert '{value}' to a boolean.");
                }

                return result;
            }
        }
    }
}