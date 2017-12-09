using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bahkat.Models
{
    class AssemblyVersion : IComparable<AssemblyVersion>
    {
        // <major version>.<minor version>.<build number>.<revision>
        private static readonly Regex _regex = new Regex(@"^(\d+).(\d+).(\d+).(\d+)$");
        private int[] _parts;

        public int Major => _parts[0];
        public int Minor => _parts[1];
        public int Build => _parts[2];
        public int Revision => _parts[3];
        
        internal static AssemblyVersion Create(string raw)
        {
            if (!_regex.IsMatch(raw))
            {
                return null;
            }

            return new AssemblyVersion()
            {
                _parts = raw.Split('.').Select(int.Parse).ToArray()
            };
        }

        public int CompareTo(AssemblyVersion other)
        {
            for (int i = 0; i < _parts.Length; ++i)
            {
                var x = _parts[i].CompareTo(other._parts[i]);
                if (x != 0)
                {
                    return x;
                }
            }

            return 0;
        }
    }
}