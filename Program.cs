using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using static CRC32.CRC32;
using static CRC32.CRC32.ErrorCodes;
namespace CRC32
{
    class Program : NativeMethods
    {
        static void Main()
        {
            IntPtr hKernelbase = Libloaderapi.GetModuleHandleA("KERNELBASE.dll");
            IntPtr isDebuggerPresentAddr = Libloaderapi.GetProcAddress(hKernelbase, "IsDebuggerPresent");

            int c = 1;
            while (true)
            {

                ErrorCodes crcVal = DynamicAccumulateAtAddress(isDebuggerPresentAddr, 0xD, out int crcValue);
                if (crcVal != NO_ERROR)
                    Console.WriteLine($"{c}: CRC Check Failed for {isDebuggerPresentAddr}. Error Codes: {crcVal} (LastError: {Marshal.GetLastWin32Error()}");
                
                Console.WriteLine($"{c}: {crcValue} (IDP: {isDebuggerPresentAddr:X})");
                //Console.ReadLine();
                c++;
                Thread.Sleep(10);
            };

            //Free ModuleHandle.
        }
    }
}
