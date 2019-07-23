using Pahkat.Sdk.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pahkat.Sdk.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var store = PackageStore.New("C:\\sendhelp");
                store.Config().SetUiValue("hello", "yes");
                var foo = store.Config().GetUiValue("hello");
                Console.WriteLine(foo);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

            }
            Console.ReadLine();
        }
    }
}
