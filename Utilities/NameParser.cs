using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MP.Utilities
{
    /// <summary>
    /// Provides person name parsing.
    /// </summary>
    public static class NameParser
    {
        #region Regular expressions

        private const RegexOptions Options = RegexOptions.IgnoreCase;

        /// <summary>
        /// Matches name title.
        /// </summary>
        private static readonly Regex _titleRegex = new Regex(@"\b(Mr|Mrs|Miss|Ms|Dr|Prof|Rev|Drs|Hon|Sr|Sra)\b", Options);

        /// <summary>
        /// Matches name suffix.
        /// </summary>
        private static readonly Regex _suffixRegex = new Regex(@"\b(Jr|Sr|[IVXLCM]+)\b", Options);

        /// <summary>
        /// Matches a string that could be a roman number.
        /// </summary>
        private static readonly Regex _romanNumberRegex = new Regex(@"\b[IVXLCM]+\b", Options);

        /// <summary>
        /// Matches characters that must be trimmed from the name. Any match will be replaced with a single space character.
        /// </summary>
        private static readonly Regex _trimRegex = new Regex(@"[^\p{L}\p{Nd}\p{Nl}\p{Pd}'\u2019]+", Options);

        /// <summary>
        /// Matches lowercase letter at the start or uppercase letter after any letter.
        /// </summary>
        private static readonly Regex _notFirstUpperRegex = new Regex(@"\b\p{Ll}|\B\p{Lu}", Options & ~RegexOptions.IgnoreCase);

        private static readonly string[] _lastNameOnePartPrefixes = new[] {
            "AF", "AG", "AI", "AK", "AM", "AN", "AP", "AR", "AS", "AU", "AUS", "AUX", "AZ", "D", "D'", "DA", "DAL", "DALLA", "DALLAS", 
            "DALLE", "DAS", "DE", "DES", "DEGLIA", "DEI", "DEL", "DELAH", "DELLAS", "DELLE", 
            "DELLI", "DELLO", "DEN", "DER", "DET", "DI", "DO", "DOS", "DU", "HA", "HAI", "HE", "HEIS", "HEN", "HET", "HI", "HIN", "HINAR", 
            "HINIR", "HINN", "HN", "IM", "HOI", "LA", "LAS", "LE", "LES", "LHI", "LI", "LIS", "LO", "LOS", "LOU", "LU", "MAC", 
            "MC", "MIA", "O", "O'", "ST", "SAINT", "SAN", "VAN", "VANDER", "VOM", "VON", "ZUR", "ZUM"
        };

        private static readonly string[] _lastNameTwoPartPrefixes = new[]
        {
            "DE LA", "DE LAS", "DE LO", "DE LOS", "VAN DE", "VAN DEN", "VAN DER"
        };

        #endregion

        #region Public methods

        /// <summary>
        /// Parses person name string and populates <b>Person</b> instance with parsed values.
        /// </summary>
        /// <param name="personName">Person name string that may contain first, last and middle names, name title and suffix.</param>
        /// <param name="person"><b>Person</b> instance to be populated with parsing results.</param>
        /// <param name="useEmptyStringsByDefault">If <b>true</b>, name properties that are not found in person name string will be populated with empty strings.</param>
        public static void Parse(string personName, Person person, bool useEmptyStringsByDefault = true)
        {
            if (personName == null)
                throw new ArgumentNullException("personName");
            if (person == null)
                throw new ArgumentNullException("person");

            WipeName(person, useEmptyStringsByDefault ? "" : null);

            ParseInternal(personName, person);
        }

        /// <summary>
        /// Parses person name string and returns new <b>Person</b> instance populated with parsed values.
        /// </summary>
        /// <param name="personName">Person name string that may contain first, last and middle names, name title and suffix.</param>
        /// <param name="useEmptyStringsByDefault">If <b>true</b>, name properties that are not found in person name string will be populated with empty strings.</param>
        /// <returns><b>Person</b> instance populated with parsing results.</returns>
        public static Person Parse(string personName, bool useEmptyStringsByDefault = true)
        {
            if (personName == null)
                throw new ArgumentNullException("personName");

            var person = new Person();
            if (useEmptyStringsByDefault)
                WipeName(person, "");

            ParseInternal(personName, person);

            return person;
        }

        /// <summary>
        /// Ensures name title is UpperCamelCase and closed with a period (for example, "Ms.").
        /// </summary>
        /// <param name="title">Name title string to be normalized.</param>
        /// <returns>Normalized name title.</returns>
        public static string NormalizeTitle(string title)
        {
            var text = _trimRegex.Replace(title, "");
            return _titleRegex.IsMatch(text) ? NormalizeTitleInternal(text) : title.Trim();
        }

        /// <summary>
        /// Ensures name suffix is uppercase roman number (for example, "IV"), or is UpperCamelCase and closed with a period (for example, "Jr.").
        /// </summary>
        /// <param name="suffix">Name suffix string to be normalized.</param>
        /// <returns>Normalized name suffix.</returns>
        public static string NormalizeSuffix(string suffix)
        {
            var text = _trimRegex.Replace(suffix, "");
            return _suffixRegex.IsMatch(text) ? NormalizeSuffixInternal(text) : suffix.Trim();
        }

        #endregion

        #region Private methods

        private static void ParseInternal(string personName, Person person)
        {
            // Check if person name is represented as "Last name, first name".
            personName = TryReverse(personName);

            // Normalize person name string: remove all punctuation that is not a part of any name.
            personName = _trimRegex.Replace(personName, " ").Trim();
            if (personName == "")
                return;

            var words = new List<string>(personName.Split());

            // Check if first word is a title.
            if (_titleRegex.IsMatch(words[0]))
            {
                person.Title = NormalizeTitleInternal(words[0]);
                words.RemoveAt(0);
                if (words.Count == 0)
                    return;
            }

            // Check if last word is a suffix.
            if (_suffixRegex.IsMatch(words[words.Count - 1]))
            {
                person.Suffix = NormalizeSuffixInternal(words[words.Count - 1]);
                words.RemoveAt(words.Count - 1);
                if (words.Count == 0)
                    return;
            }

            if (words.Count == 1)
            {
                if (string.IsNullOrEmpty(person.Title) && string.IsNullOrEmpty(person.Suffix))
                {
                    // Single word which is not a title or a suffix: must be a first name.
                    person.FirstName = words[0];
                }
                else
                {
                    // Single word with a title or a suffix: must be a last name.
                    person.LastName = words[0];
                }
                return;
            }

            // Last word is a last name.
            person.LastName = words[words.Count - 1];
            words.RemoveAt(words.Count - 1);

            // Look for last name prefixes.
            var prefixFound = false;
            if (words.Count > 1)
            {
                // Check if previous words are two-part last name prefix.
                foreach (var prefix in _lastNameTwoPartPrefixes)
                {
                    var parts = prefix.Split();
                    if (parts[0].Equals(words[words.Count - 2], StringComparison.OrdinalIgnoreCase) &&
                        parts[1].Equals(words[words.Count - 1], StringComparison.OrdinalIgnoreCase))
                    {
                        person.LastName = words[words.Count - 2] + " " + words[words.Count - 1] + " " + person.LastName;
                        words.RemoveRange(words.Count - 2, 2);

                        prefixFound = true;
                        break;
                    }
                }
            }
            if (!prefixFound)
            {
                // Check if previous word is a one-part last name prefix.
                foreach (var prefix in _lastNameOnePartPrefixes)
                {
                    if (prefix.Equals(words[words.Count - 1], StringComparison.OrdinalIgnoreCase))
                    {
                        person.LastName = words[words.Count - 1] + " " + person.LastName;
                        words.RemoveAt(words.Count - 1);
                        break;
                    }
                }
            }
            if (words.Count == 0)
                return;

            if (words.Count > 1)
            {
                // First word is a first name.
                person.FirstName = words[0];
                words.RemoveAt(0);

                // Next word is a middle name.
                person.MiddleName = words[0];
                words.RemoveAt(0);

                // If any words remain, add them to middle name.
                while (words.Count > 0)
                {
                    person.MiddleName += " " + words[0];
                    words.RemoveAt(0);
                }
            }
            else
            {
                // Single remaining word is a first name.

                person.FirstName = words[0];
            }
        }

        private static string TryReverse(string personName)
        {
            var index = personName.IndexOf(',');

            if (index < 0)
                return personName;

            if (index < personName.Length - 1)
                return personName.Substring(index + 1) + " " + personName.Substring(0, index);

            return personName.Substring(0, index);
        }

        private static string NormalizeTitleInternal(string title)
        {
            title = ToFirstUpper(title);

            return title == "Miss" ? title : title + ".";
        }

        private static string NormalizeSuffixInternal(string suffix)
        {
            return _romanNumberRegex.IsMatch(suffix) ? suffix.ToUpper() : ToFirstUpper(suffix) + ".";
        }

        private static string ToFirstUpper(string text)
        {
            if (_notFirstUpperRegex.IsMatch(text))
                return text.Substring(0, 1).ToUpper() + text.Substring(1).ToLower();

            return text;
        }

        private static void WipeName(Person person, string wipeString)
        {
            person.Title = wipeString;
            person.FirstName = wipeString;
            person.MiddleName = wipeString;
            person.LastName = wipeString;
            person.Suffix = wipeString;
        }

        #endregion
    }
}
