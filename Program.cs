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
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting...");
                h.DeleteHandle();
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
        detect:
            Thread.Sleep(1000);
            Process[] rblxClients = Process.GetProcessesByName("RobloxPlayerBeta");
            if(rblxClients.Length > clientCount)
            {
                if (rblxClients.Length - clientCount > 1)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {rblxClients.Length - clientCount} clients were opened. {rblxClients.Length} Client(s) open.");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] A client was opened. {rblxClients.Length} Client(s) open.");
                }
                Thread.Sleep(5000);
                h.DeleteHandle();
            }
            else if(rblxClients.Length < clientCount)
            {
                if (clientCount - rblxClients.Length > 1)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {rblxClients.Length - clientCount} clients were closed. {rblxClients.Length} Client(s) open.");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] A client was closed. {rblxClients.Length} Client(s) open.");
                }
            }
            clientCount = rblxClients.Length;
            goto detect;
        }
    }
}
