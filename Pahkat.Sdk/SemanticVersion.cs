using System;

namespace Pahkat.Sdk
{
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        private string _versionString;

        public SemanticVersion(string versionString)
        {
            if (Native.pahkat_semver_is_valid(MarshalUtf8.StringToHGlobalUtf8(versionString)) == 0)
            {
                throw new ArgumentException($"Failed to parse the semver version string ${versionString}");
            }
            _versionString = versionString;
        }

        public override string ToString()
        {
            return _versionString;
        }

        public int CompareTo(SemanticVersion other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return 1;
            }

            var lhs = MarshalUtf8.StringToHGlobalUtf8(_versionString);
            var rhs = MarshalUtf8.StringToHGlobalUtf8(other.ToString());

            return Native.pahkat_semver_compare(lhs, rhs);
        }
    }
}
