using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
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

                int crcVal = CRC32.AccumulateAtAddress(isDebuggerPresentAddr, 15);

                Console.WriteLine($"{c}: {crcVal} (IDP: {isDebuggerPresentAddr:X})");
                Console.ReadLine();
                c++;
            };

            //Free ModuleHandle.
        }
    }
}
