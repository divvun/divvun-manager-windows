using System.Collections.Generic;

namespace Pahkat.Sdk.Rpc
{
    public class ResolvedAction
    {
        public PackageAction Action;
        public Dictionary<string, string> Name;
        public string Version;
    }
}