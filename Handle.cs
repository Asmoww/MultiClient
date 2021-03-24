using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MultiClient
{
    class Handle
    {
        private class WS
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SYSTEM_HANDLE_INFORMATION
            {
                public int ProcessID;
                public byte ObjectTypeNumber;
                public byte Flags;
                public ushort Handle;
                public int Object_Pointer;
                public UInt32 GrantedAccess;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_BASIC_INFORMATION
            {
                public int Attributes;
                public int GrantedAccess;
                public int HandleCount;
                public int PointerCount;
                public int PagedPoolUsage;
                public int NonPagedPoolUsage;
                public int Reserved1;
                public int Reserved2;
                public int Reserved3;
                public int NameInformationLength;
                public int TypeInformationLength;
                public int SecurityDescriptorLength;
                public System.Runtime.InteropServices.ComTypes.FILETIME CreateTime;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_NAME_INFORMATION
            {
                public UNICODE_STRING Name;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct UNICODE_STRING
            {
                public ushort Length;
                public ushort MaximumLength;
                public IntPtr Buffer;
            }
        }

        #region DLL Imports
        [DllImport("ntdll.dll")]
        private static extern uint NtQuerySystemInformation(uint SystemInformationClass, IntPtr SystemInformation,
            int SystemInformationLength, ref int nLength);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(IntPtr hObject);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryObject(IntPtr Handle, int ObjectInformationClass, IntPtr ObjectInformation,
            int ObjectInformationLength, ref int returnLength);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, ushort hSourceHandle, IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        #endregion
        private static string ViewHandleName(WS.SYSTEM_HANDLE_INFORMATION shHandle, Process process)
        {
            IntPtr sourceProcessHandle = OpenProcess(0x1F0FFF, false, process.Id);
            IntPtr targetHandle = IntPtr.Zero;

            if (!DuplicateHandle(sourceProcessHandle, shHandle.Handle, GetCurrentProcess(), out targetHandle, 0, false, 0x2))
            {
                return null;
            }

            IntPtr basicQueryData = IntPtr.Zero;

            WS.OBJECT_BASIC_INFORMATION basicInformationStruct = new WS.OBJECT_BASIC_INFORMATION();
            WS.OBJECT_NAME_INFORMATION nameInformationStruct = new WS.OBJECT_NAME_INFORMATION();

            basicQueryData = Marshal.AllocHGlobal(Marshal.SizeOf(basicInformationStruct));

            int nameInfoLength = 0;
            NtQueryObject(targetHandle, 0, basicQueryData, Marshal.SizeOf(basicInformationStruct), ref nameInfoLength);

            basicInformationStruct = (WS.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(basicQueryData, basicInformationStruct.GetType());
            Marshal.FreeHGlobal(basicQueryData);

            nameInfoLength = basicInformationStruct.NameInformationLength;

            IntPtr nameQueryData = Marshal.AllocHGlobal(nameInfoLength);

            int result;
            while ((uint)(result = NtQueryObject(targetHandle, 1, nameQueryData, nameInfoLength, ref nameInfoLength)) == 0xc0000004)
            {
                Marshal.FreeHGlobal(nameQueryData);
                nameQueryData = Marshal.AllocHGlobal(nameInfoLength);
            }
            nameInformationStruct = (WS.OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(nameQueryData, nameInformationStruct.GetType());

            IntPtr handlerName;

            if (Is64Bits())
            {
                handlerName = new IntPtr(Convert.ToInt64(nameInformationStruct.Name.Buffer.ToString(), 10) >> 32);
            }
            else
            {
                handlerName = nameInformationStruct.Name.Buffer;
            }
            if (handlerName != IntPtr.Zero)
            {
                byte[] baTemp2 = new byte[nameInfoLength];
                try
                {
                    Marshal.Copy(handlerName, baTemp2, 0, nameInfoLength);
                    return Marshal.PtrToStringUni(Is64Bits() ? new IntPtr(handlerName.ToInt64()) : new IntPtr(handlerName.ToInt32()));
                }
                catch (AccessViolationException)
                {
                    return null;
                }
                finally
                {
                    //Console.WriteLine("Accessviolation caught!");
                    Marshal.FreeHGlobal(nameQueryData);
                    CloseHandle(targetHandle);
                }
            }
            return null;
        }

        private async Task CloseProcessHandles(Process growtopia)
        {
            Console.WriteLine("Querying system handle information...");

            int nLength = 0;
            IntPtr handlePointer = IntPtr.Zero;
            int sysInfoLength = 0x10000;
            IntPtr infoPointer = Marshal.AllocHGlobal(sysInfoLength);

            uint result;
            while ((result = NtQuerySystemInformation(0x10, infoPointer, sysInfoLength, ref nLength)) == 0xc0000004)
            {
                sysInfoLength = nLength;
                Marshal.FreeHGlobal(infoPointer);
                infoPointer = Marshal.AllocHGlobal(nLength);
            }

            byte[] baTemp = new byte[nLength];
            Marshal.Copy(infoPointer, baTemp, 0, nLength);

            long sysHandleCount = 0;
            if (Is64Bits())
            {
                sysHandleCount = Marshal.ReadInt64(infoPointer);
                handlePointer = new IntPtr(infoPointer.ToInt64() + 8);
            }
            else
            {
                sysHandleCount = Marshal.ReadInt32(infoPointer);
                handlePointer = new IntPtr(infoPointer.ToInt32() + 4);
            }

            Console.WriteLine("Processing" + sysHandleCount + " results...");

            WS.SYSTEM_HANDLE_INFORMATION handleInfoStruct;

            List<WS.SYSTEM_HANDLE_INFORMATION> handles = new List<WS.SYSTEM_HANDLE_INFORMATION>();
            int mutexNum = 0;
            for (long i = 0; i < sysHandleCount; i++)
            {
                handleInfoStruct = new WS.SYSTEM_HANDLE_INFORMATION();
                if (Is64Bits())
                {
                    handleInfoStruct = (WS.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(handlePointer, handleInfoStruct.GetType());
                    handlePointer = new IntPtr(handlePointer.ToInt64() + Marshal.SizeOf(handleInfoStruct) + 8);
                }
                else
                {
                    handleInfoStruct = (WS.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(handlePointer, handleInfoStruct.GetType());
                    handlePointer = new IntPtr(handlePointer.ToInt64() + Marshal.SizeOf(handleInfoStruct));
                }

                if (handleInfoStruct.ProcessID != growtopia.Id)
                {
                    continue;
                }

                string handleName = ViewHandleName(handleInfoStruct, growtopia);

                if (handleName != null && handleName.StartsWith(@"\Sessions\") && handleName.EndsWith(@"\BaseNamedObjects\ROBLOX_singletonEvent"))
                {
                    mutexNum++;
                    handles.Add(handleInfoStruct);
                    Console.WriteLine($"Found mutex [{handleInfoStruct.ProcessID}] [{mutexNum}]");
                }
                else
                {
                    continue;
                }

            }
            Console.WriteLine("Closing mutexes...");
            mutexNum = 0;
            foreach (WS.SYSTEM_HANDLE_INFORMATION handle in handles)
            {
                mutexNum++;
                CloseMutex(handle, mutexNum);
            }
        }

        /*private async Task SuspendProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(0x0002, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
            Console.WriteLine($"Suspended process [{process.Id}]");
        }

        private void ResumeProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(0x0002, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }*/

        private async Task CloseMutex(WS.SYSTEM_HANDLE_INFORMATION handle, int mutexNum)
        {
            IntPtr targetHandle;
            if (!DuplicateHandle(Process.GetProcessById(handle.ProcessID).Handle, handle.Handle, IntPtr.Zero, out targetHandle, 0, false, 0x1))
            {
                Console.WriteLine($"Failed to close mutex [{handle.ProcessID}]: " + Marshal.GetLastWin32Error());
            }
            else
            {
                Console.WriteLine($"Closed mutex [{handle.ProcessID}] [{mutexNum}]");
            }
        }
        private static bool Is64Bits()
        {
            return Marshal.SizeOf(typeof(IntPtr)) == 8 ? true : false;
        }
        public void OpenRblx()
        {
            Process[] rblxClient = Process.GetProcessesByName("RobloxPlayerBeta");
            if (rblxClient.Length != 0)
            {
                CloseProcessHandles(rblxClient[0]);
            }
            else
            {
                Console.WriteLine("No process found");
            }
        }
    }
}

