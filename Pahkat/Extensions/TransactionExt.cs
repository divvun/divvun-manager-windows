using Newtonsoft.Json;
using Pahkat.Sdk;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Pahkat.Extensions
{
    public static class TransactionExt
    {
        public static Transaction FromActionsFile(string actionsPath)
        {
            var str = File.ReadAllText(actionsPath);
            var actions = JsonConvert.DeserializeObject<List<TransactionAction>>(str);
            var packageStore = ((PahkatApp)Application.Current).PackageStore;
            return Transaction.New(packageStore, actions);
        }
    }
}
