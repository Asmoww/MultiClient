using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiClient
{
    class Program
    {
        static void Main()
        {
            Handle h = new Handle();
            string args = Console.ReadLine();
            if (args == "run")
            {
                Console.WriteLine("Running...");
                h.OpenRblx();
            }
        }
    }
}
