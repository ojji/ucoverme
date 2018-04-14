using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;

namespace UCoverme.Utils
{
    public static class CommandOptionExtensions
    {
        /// <summary>
        /// Indicates the option is required.
        /// </summary>
        /// <param name="option">The option.</param>
        /// <param name="allowEmptyStrings">Indicates whether an empty string is allowed.</param>
        /// <param name="errorMessage">The custom error message to display. See also <seealso cref="ValidationAttribute.ErrorMessage"/>.</param>
        /// <returns>The option.</returns>
        public static T IsRequired<T>(this T option, bool allowEmptyStrings = false, string errorMessage = null)
            where T : CommandOption
        {
            var attribute = GetValidationAttr<RequiredAttribute>(errorMessage);
            attribute.AllowEmptyStrings = allowEmptyStrings;
            option.Validators.Add(new AttributeValidator(attribute));
            return option;
        }

        private static T GetValidationAttr<T>(string errorMessage, object[] ctorArgs = null)
            where T : ValidationAttribute
        {
            var attribute = (T)Activator.CreateInstance(typeof(T), ctorArgs ?? new object[0]);
            if (errorMessage != null)
            {
                attribute.ErrorMessage = errorMessage;
            }
            return attribute;
        }
    }
}