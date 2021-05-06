using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CRC32
{
    class Program : NativeMethods
    {
        delegate int TestDelegate(IntPtr isDebuggerPresentAddr);
        static IntPtr allocLoc;
        static readonly byte[] crcCheckCode = new byte[]
        {
            0x48, 0x31, 0xc0,                               //xor rax, rax
            0x48, 0x8b, 0x49, 0x09,                         //mov rcx, [rcx+8]
            0xf2, 0x48, 0x0f, 0x38, 0xf1, 0xc1,             //crc32 rax, rcx
            0xc3                                            //ret
        };
        static void Main()
        {            
            IntPtr hKernelbase = Libloaderapi.GetModuleHandleA("KERNELBASE.dll");
            IntPtr isDebuggerPresentAddr = Libloaderapi.GetProcAddress(hKernelbase, "IsDebuggerPresent");

            
            while (true)
            {
                int c = 1;
                int crcVal = CRC32.AccumulateAtAddress(isDebuggerPresentAddr, 16);
              
                Console.WriteLine($"{c}: {crcVal} (IDP: {isDebuggerPresentAddr.ToString("X")})");
                Console.ReadLine();

            };

            //Free ModuleHandle.
        
    }
}
