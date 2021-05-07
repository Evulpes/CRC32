using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using static CRC32.CRC32;
using static CRC32.CRC32;
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

                int crcVal = CRC32.DynamicAccumulateAtAddress(isDebuggerPresentAddr, 1);

                Console.WriteLine($"{c}: {crcVal} (IDP: {isDebuggerPresentAddr:X})");
                Console.ReadLine();
                c++;
            };

            //Free ModuleHandle.
        }
    }
}
