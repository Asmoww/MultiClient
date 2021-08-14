using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MultiClient
{
    class G
    {
        public static bool log = false;
        public static bool afk = false;
    }
    class Program
    {
        private const int SW_MINIMIZE = 6;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        public static Process GetForegroundProcess()
        {
            uint processID = 0;
            IntPtr hWnd = GetForegroundWindow(); 
            uint threadID = GetWindowThreadProcessId(hWnd, out processID); 
            Process fgProc = Process.GetProcessById(Convert.ToInt32(processID)); 
            return fgProc;
        }

        static void Main()
        {
            Handle h = new Handle();
            Task.Run(() => DetectClient());
            Task.Run(() => Afk());
            Task.Run(() => Timer());
            Console.Title = "MultiClient";
            Console.WriteLine("run - Delete mutex manually");
            Console.WriteLine("log - Show additional logs/info");
            Console.WriteLine("afk - Cycle between clients, you need an autoclicker or similar");
            Console.WriteLine(" ");
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
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Logs enabled.");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Logs disabled.");
                }
            }
            else if (args == "afk")
            {
                G.afk = !G.afk;
                if (G.afk)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] AFK mode on.");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] AFK mode off.");
                }
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
            string userName = Environment.UserName;
            Handle h = new Handle();
            Process[] existingClients = Process.GetProcessesByName("RobloxPlayerBeta");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Hello, user :)");
            if (existingClients.Length == 1)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {existingClients.Length} Client is open.");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {existingClients.Length} Clients are open.");
            }
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
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {rblxClients.Length - clientCount} clients were opened. {rblxClients.Length} Clients are open.");
                }
                else
                {
                    if (rblxClients.Length == 1)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] A client was opened. {rblxClients.Length} Client is open.");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] A client was opened. {rblxClients.Length} Clients are open.");
                    }
                }
                Thread.Sleep(5000);
                h.DeleteHandle(G.log, false);
            }
            else if(rblxClients.Length < clientCount)
            {
                if (clientCount - rblxClients.Length > 1)
                {
                    if (rblxClients.Length == 1)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {clientCount - rblxClients.Length} Clients were closed. {rblxClients.Length} Client is open.");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {clientCount - rblxClients.Length} Clients were closed. {rblxClients.Length} Clients are open.");
                    }
                }
                else
                {
                    if (rblxClients.Length == 1)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] A client was closed. {rblxClients.Length} Client is open.");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] A client was closed. {rblxClients.Length} Clients are open.");
                    }
                }
            }
            clientCount = rblxClients.Length;
            Thread.Sleep(100);
            goto detect;
        }

        public async static Task Afk()
        {
            Thread.Sleep(5000);
        start:
            Process[] rblxClients = Process.GetProcessesByName("RobloxPlayerBeta");
            Thread.Sleep(1000);
            if (G.afk)
            {
                foreach (Process p in rblxClients)
                {
                    if (GetForegroundProcess().Id == p.Id)
                    {
                        if (G.log) Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Process {p.Id} in foreground, skipping.");
                    }
                    else
                    {
                        if (G.log) Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sending AFK input to {p.Id}...");
                        IntPtr windowHandle = p.MainWindowHandle;
                        SwitchToThisWindow(windowHandle, true);
                        SetForegroundWindow(windowHandle);
                        //input, maybe in the future if I can find a way
                        Thread.Sleep(500);
                        ShowWindow(windowHandle, SW_MINIMIZE);
                        Thread.Sleep(20);
                    }
                }
                for (int i = 0; i < 10; i++)
                {
                    if (!G.afk)
                    {
                        goto start;
                    }
                    Thread.Sleep(1000);                   
                }
            }       
            goto start;
        }

        public async static Task Timer()
        {
        start:
            Thread.Sleep(1000);
            Process[] rblxClients = Process.GetProcessesByName("RobloxPlayerBeta");
            foreach (Process p in rblxClients)
            {
                if (!p.HasExited)
                {
                    try
                    {
                        if (p.MainWindowTitle == "Roblox" && p.ProcessName == "RobloxPlayerBeta")
                        {
                            SetWindowText(p.MainWindowHandle, "Roblox - 1 seconds");
                        }
                        else
                        {
                            int windowTime = Int32.Parse(p.MainWindowTitle.Replace("Roblox - ", "").Replace(" seconds", "")) + 1;
                            SetWindowText(p.MainWindowHandle, "Roblox - " + windowTime + " seconds");
                        }
                    }
                    catch (Exception)
                    {
                        //if (G.log) Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: Unable to set the title for clients. Client may be closing?");
                        //useless to log, happens every time a client is opened or closed
                    }
                }
                else
                {
                    if (G.log) Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client is probably closing, ignoring.");
                }
            }
            goto start;
        }
    }
}
