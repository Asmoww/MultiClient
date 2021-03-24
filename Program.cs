using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MultiClient
{
    class G
    {
        public static bool log = false;
    }
    class Program
    {
        static void Main()
        {
            Handle h = new Handle();
            Task.Run(() => DetectClient());
        start:
            string args = Console.ReadLine();
            if (args == "run")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Running manually...");
                h.DeleteHandle(G.log, true);
            }
            else if (args == "log")
            {
                G.log = !G.log;
                if (G.log)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Logging enabled.");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Logging disabled.");
                }
            }
            else if (args == "clean")
            {
                GC.Collect();
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Invalid command.");
            }
            goto start;
        }

        public async static Task DetectClient()
        {
            int clientCount = 0;
            Handle h = new Handle();
            Process[] existingClients = Process.GetProcessesByName("RobloxPlayerBeta");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {existingClients.Length} Client(s) are open.");
            clientCount = existingClients.Length;
            if(existingClients.Length > 0)
            {
                h.DeleteHandle(false, true);
            }
        detect:
            Process[] rblxClients = Process.GetProcessesByName("RobloxPlayerBeta");
            if (rblxClients.Length > clientCount)
            {
                if (rblxClients.Length - clientCount > 1)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {rblxClients.Length - clientCount} clients were opened. {rblxClients.Length} Client(s) are open.");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] A client was opened. {rblxClients.Length} Client(s) open.");
                }
                Thread.Sleep(5000);
                h.DeleteHandle(G.log, false);
            }
            else if(rblxClients.Length < clientCount)
            {
                if (clientCount - rblxClients.Length > 1)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {rblxClients.Length - clientCount} clients were closed. {rblxClients.Length} Client(s) are open.");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] A client was closed. {rblxClients.Length} Client(s) open.");
                }
            }
            clientCount = rblxClients.Length;
            Thread.Sleep(500);
            goto detect;
        }
    }
}
