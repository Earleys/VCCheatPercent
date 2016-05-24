using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VCSplitInfo
{


    class Memory
    {
        // This is a class I made a while ago with a lot of (unnecessary) memory stuff. I don't even know what they all do, but they're here so you can use them if you want.
        // - Earleys
        

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }

        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;
        private static IntPtr ProcessHandle;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("Kernel32.dll")]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UInt32 nSize, ref UInt32 lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hProcess);
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress,
          byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        public static byte[] ReadMemory(Process process, long address, int numOfBytes, out int bytesRead)
        {
            IntPtr hProc = OpenProcess(ProcessAccessFlags.All, false, process.Id);     
            byte[] buffer = new byte[numOfBytes];
            ReadProcessMemory(hProc, new IntPtr(address), buffer, numOfBytes, out bytesRead);
           
            return buffer;
        }

        public static bool writeToMemory(Process p, int address, byte[] value)
        {
            try
            {
                var hProc = OpenProcess(ProcessAccessFlags.All, false, (int)p.Id);
              //  var val = new byte { value };

                int wtf = 0;
                WriteProcessMemory(hProc, new IntPtr(address), value, (UInt32)value.LongLength, out wtf);

                CloseHandle(hProc);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static int getMemoryResult(Process process, long address, int _byte)
        {

            int bytesRead;
            byte[] memoryOutput = Memory.ReadMemory(process, address, _byte, out bytesRead);
            int value = BitConverter.ToInt32(memoryOutput, 0);
            return value;
        }

      /*  public static long getMemoryResultInt(Process process, long address, int _byte)
        {
            try
            {
                int bytesRead;
                byte[] memoryOutput = Memory.ReadMemory(process, address, _byte, out bytesRead);
                long value = BitConverter.ToInt32(memoryOutput, 0);
                return value;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        */
        public static int convertStringToInt(string memoryAddress)
        {
            int mem = Convert.ToInt32(memoryAddress);
            return mem;
        }

       internal static float readFloat(int address)
       {
           bool success;
           byte[] buffer = new byte[4];
           UInt32 nBytesRead = 0;
           success = Memory.ReadProcessMemory(ProcessHandle, (IntPtr)address, buffer, 4, ref nBytesRead);
           return BitConverter.ToSingle(buffer, 0);
       }
  
    }
}
