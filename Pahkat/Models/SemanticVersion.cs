using System;
using System.Text.RegularExpressions;

namespace Pahkat.Models
{
    class SemanticVersion : IComparable<SemanticVersion>
    {
        private static readonly Regex _regex = new Regex(@"^([0-9]+)\.([0-9]+)\.([0-9]+)(?:-([0-9A-Za-z-\.]+))?$");

        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;
        public readonly string Extra;
        
        private SemanticVersion(int major, int minor, int patch, string extra = "")
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Extra = extra;
        }

        public override string ToString()
        {
            // TODO: do this properly
            return $"{Major}.{Minor}.{Patch}";
        }

        internal static SemanticVersion Create(string raw)
        {
            var match = _regex.Match(raw);

            if (!match.Success)
            {
                return null;
            }

            var major = int.Parse(match.Groups[1].Value);
            var minor = int.Parse(match.Groups[2].Value);
            var patch = int.Parse(match.Groups[3].Value);
            
            if (match.Groups[4].Value == "")
            {
                return new SemanticVersion(major, minor, patch);
            }
            
            var extra = match.Groups[4].Value;
            return new SemanticVersion(major, minor, patch, extra);
        }
        
        public int CompareTo(SemanticVersion other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return 1;
            }

            var majorRes = Major.CompareTo(other.Major);

            if (majorRes != 0)
            {
                return majorRes;
            }

            var minorRes = Minor.CompareTo(other.Minor);

            if (minorRes != 0)
            {
                return minorRes;
            }

            var patchRes = Patch.CompareTo(other.Patch);

            if (patchRes != 0)
            {
                return patchRes;
            }
            
            // Meta parsing is a pain

            if (other.Extra == Extra)
            {
                return 0;
            }

            if (Extra == "")
            {
                return 1;
            }

            if (other.Extra == "")
            {
                return -1;
            }
            
            // If we get to this point, we have to handle precedence according to p11 of the semver 2.0.0 spec.
            // TODO: actually do that.
            return String.Compare(Extra, other.Extra, StringComparison.Ordinal);
        }
    }
}