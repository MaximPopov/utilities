using System;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;

namespace MP.Utilities
{
    /// <summary>
    /// Provides street address string parsing.
    /// </summary>
    public static class AddressParser
    {
        #region Regular expressions

        private const RegexOptions Options = RegexOptions.IgnoreCase;

        /// <summary>
        /// Matches floor number or name.
        /// </summary>
        private static readonly Regex[] _floorRegexes = new[]
        {
            new Regex(@"(?<=^\s*|\W\s*)floor\s+(\d{1,3}|[a-z]\d|[a-z]+)\b", Options),
            new Regex(@"\b(\d{1,3})(?:st|nd|rd|th)?\s+floor\b", Options),
            new Regex(@"\b(\d{1,3}|[a-z]\d|[a-z]+)\s+floor\b", Options)
        };

        /// <summary>
        /// Matches unit number.
        /// </summary>
        private static readonly Regex[] _unitNumberRegexes = new[]
        {
            new Regex(@"(?:apt|unit|suite|apartment|#)\W*(\d+(-?[a-z])?)\b", Options),
            new Regex(@"(?<=^\s*)(\d+(-?[a-z])?)\s*\-", Options)
        };

        /// <summary>
        /// Matches house number.
        /// </summary>
        private static readonly Regex _houseNumberRegex =
            new Regex(@"\b\d+\b", Options);

        /// <summary>
        /// Matches street designator (word characters only, without punctuation).
        /// </summary>
        private static readonly Regex _streetDesignatorRegex; // created in static constructor.

        /// <summary>
        /// Matches direction prefix.
        /// </summary>
        private static readonly Regex _directionPrefixRegex = 
            new Regex(@"(?<=^\W*)(N|NE|E|SE|S|SW|W|NW)\b", Options);

        /// <summary>
        /// Matches direction suffix.
        /// </summary>
        private static readonly Regex _directionSuffixRegex = 
            new Regex(@"\b(N|NE|E|SE|S|SW|W|NW)(?=\s*(\P{L}|$))", Options);

        /// <summary>
        /// Matches street name.
        /// </summary>
        private static readonly Regex _streetNameRegex =
            new Regex(@"[\p{L}\d]([\p{L}\d\s'\-]*[\p{L}\d])?", Options);

        /// <summary>
        /// Matches lowercase letter at the start or uppercase letter after any letter.
        /// </summary>
        private static readonly Regex _notFirstUpperRegex = new Regex(@"\b\p{Ll}|\B\p{Lu}", Options & ~RegexOptions.IgnoreCase);

        /// <summary>
        /// Maps street designator abbreviations to standard TP8i street designators.
        /// </summary>
        private static readonly StringDictionary _streetDesignatorAbbreviations = new StringDictionary(); // StringDictionary is chosen because of case-insensitive key search.

        static AddressParser()
        {
            // Create street designator regex and abbreviation mapping.

            // First designator in a line is a standard TP8i designator, others are abbreviations of the same designator.
            const string streetDesignators = @"
                Alley
                Annex
                Arcade
                Avenue ave
                Beach
                Boulevard blvd
                Bypass
                Cape
                Cay
                Circle
                Court
                Cove
                Creek
                Crescent
                Crest
                Crossing
                Crossroad
                Drive dr
                Estate
                Expressway
                Extension
                Freeway
                Heights
                Highway
                Junction
                Lane
                Loop
                Manor
                Park
                Parkway
                Passage
                Place
                Plaza
                Point
                Road rd
                Square sq
                Station
                Street st str
                Terrace
                Trace
                Trail
                Turnpike
                View
                Way
            ";

            var sb = new StringBuilder();
            foreach (var designatorFamily in streetDesignators.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
            {
                var designators = designatorFamily.Trim().Split();
                if (designators[0].Length == 0)
                    continue; // skip empty line

                for (var index = 0; index < designators.Length; index++)
                {
                    sb.Append('|').Append(designators[index]);
                    if (index > 0)
                        _streetDesignatorAbbreviations.Add(designators[index], designators[0]);
                }
            }
            sb.Remove(0, 1);
            sb.Insert(0, @"\b(");
            sb.Append(@")\b");

            _streetDesignatorRegex = new Regex(sb.ToString(), Options);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Parses address string and populates <b>Address</b> instance with parsed values.
        /// </summary>
        /// <param name="addressString">Address string that may contain unit and house numbers, street name and floor number.</param>
        /// <param name="address"><b>Address</b> instance to be populated with parsing results.</param>
        /// <param name="useEmptyStringsByDefault">If <b>true</b>, address properties that are not found in address string will be populated with empty strings.</param>
        public static void Parse(string addressString, Address address, bool useEmptyStringsByDefault = true)
        {
            if (addressString == null)
                throw new ArgumentNullException("addressString");
            if (address == null)
                throw new ArgumentNullException("address");

            WipeAddress(address, useEmptyStringsByDefault ? "" : null);

            ParseInternal(addressString, address);
        }

        /// <summary>
        /// Parses address string and returns new <b>Address</b> instance populated with parsed values.
        /// </summary>
        /// <param name="addressString">Address string that may contain unit and house numbers, street name and floor number.</param>
        /// <param name="useEmptyStringsByDefault">If <b>true</b>, address properties that are not found in address string will be populated with empty strings.</param>
        /// <returns><b>Address</b> instance populated with parsing results.</returns>
        public static Address Parse(string addressString, bool useEmptyStringsByDefault = true)
        {
            if (addressString == null)
                throw new ArgumentNullException("addressString");

            var address = new Address();
            if (useEmptyStringsByDefault)
                WipeAddress(address, "");

            ParseInternal(addressString, address);

            return address;
        }

        /// <summary>
        /// Removes unit number designators (such as 'unit', 'apt' or '#').
        /// </summary>
        /// <param name="unitNumber">String containing raw unit number.</param>
        /// <returns>Sanitized unit number, stripped of unit designators.</returns>
        public static string TrimUnitNumber(string unitNumber)
        {
            if (string.IsNullOrEmpty(unitNumber))
                return unitNumber;

            foreach (var unitNumberRegex in _unitNumberRegexes)
            {
                var match = unitNumberRegex.Match(unitNumber);
                if (match.Success)
                    return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
            }

            return unitNumber;
        }

        /// <summary>
        /// Removes floor number designators (such as 'floor').
        /// </summary>
        /// <param name="floorNumber">String containing raw floor number.</param>
        /// <returns>Sanitized floor number, stripped of floor designators.</returns>
        public static string TrimFloorNumber(string floorNumber)
        {
            if (string.IsNullOrEmpty(floorNumber))
                return floorNumber;

            foreach (var floorRegex in _floorRegexes)
            {
                var match = floorRegex.Match(floorNumber);
                if (match.Success)
                    return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
            }

            return floorNumber;
        }

        #endregion

        #region Private methods

        private static void ParseInternal(string addressString, Address address)
        {
            // Floor number or name.
            var match = TryParseFloor(addressString, address);
            RemoveMatch(ref addressString, match);

            // Unit number.
            match = TryParseUnitNumber(addressString, address);
            RemoveMatch(ref addressString, match);

            // Street number.
            match = TryParseHouseNumber(addressString, address);
            RemoveMatch(ref addressString, match);

            // Street designator.
            match = TryParseStreetDesignator(addressString, address);
            RemoveMatch(ref addressString, match);

            // Street direction prefix.
            match = TryParseDirectionPrefix(addressString, address);
            RemoveMatch(ref addressString, match);

            // Street direction suffix.
            match = TryParseDirectionSuffix(addressString, address);
            RemoveMatch(ref addressString, match);

            // Street name.
            match = _streetNameRegex.Match(addressString);
            address.StreetName = (match.Success ? match.Value : addressString).Trim();
        }

        private static Match TryParseFloor(string addressString, Address address)
        {
            foreach (var floorRegex in _floorRegexes)
            {
                var match = floorRegex.Match(addressString);
                if (match.Success)
                {
                    address.Floor = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    return match;
                }
            }

            return null;
        }

        private static Match TryParseUnitNumber(string addressString, Address address)
        {
            foreach (var unitNumberRegex in _unitNumberRegexes)
            {
                var match = unitNumberRegex.Match(addressString);
                if (match.Success)
                {
                    address.UnitNumber = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    return match;
                }
            }

            return null;
        }

        private static Match TryParseHouseNumber(string addressString, Address address)
        {
            var match = _houseNumberRegex.Match(addressString);

            if (match.Success)
            {
                address.HouseNumber = match.Value;
            }

            return match;
        }

        private static Match TryParseStreetDesignator(string addressString, Address address)
        {
            var match = _streetDesignatorRegex.Match(addressString);

            if (match.Success)
            {
                address.StreetDesignator =
                    _streetDesignatorAbbreviations.ContainsKey(match.Value)
                        ? _streetDesignatorAbbreviations[match.Value]
                        : ToFirstUpper(match.Value);
            }

            return match;
        }

        private static Match TryParseDirectionPrefix(string addressString, Address address)
        {
            var match = _directionPrefixRegex.Match(addressString);

            if (match.Success)
            {
                address.StreetDirPrefix = match.Value.ToUpper();
            }

            return match;
        }

        private static Match TryParseDirectionSuffix(string addressString, Address address)
        {
            var match = _directionSuffixRegex.Match(addressString);

            if (match.Success)
            {
                address.StreetDirSuffix = match.Value.ToUpper();
            }

            return match;
        }

        private static void RemoveMatch(ref string address, Group match)
        {
            if (match != null && match.Success)
                address = address.Remove(match.Index, match.Length);
        }

        private static string ToFirstUpper(string text)
        {
            if (_notFirstUpperRegex.IsMatch(text))
                return text.Substring(0, 1).ToUpper() + text.Substring(1).ToLower();

            return text;
        }

        private static void WipeAddress(Address address, string wipeString)
        {
            address.UnitNumber = wipeString;
            address.HouseNumber = wipeString;
            address.StreetDirPrefix = wipeString;
            address.StreetName = wipeString;
            address.StreetDesignator = wipeString;
            address.StreetDirSuffix = wipeString;
            address.Floor = wipeString;
        }

        #endregion
    }
}