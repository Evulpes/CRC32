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



            allocLoc = Memoryapi.VirtualAlloc(IntPtr.Zero, (uint)crcCheckCode.Length, Winnt.AllocationType.MEM_COMMIT, Winnt.MemoryProtection.PAGE_EXECUTE_READWRITE);
            if (allocLoc == IntPtr.Zero)
                throw new Exception($"Failed on VirtualAlloc with System Error Code {Marshal.GetLastWin32Error()}");

            if(!Memoryapi.WriteProcessMemory(Process.GetCurrentProcess().Handle, allocLoc, crcCheckCode, crcCheckCode.Length, out _))
                throw new Exception($"Failed on WriteProcessMemory with System Error Code {Marshal.GetLastWin32Error()}");

            Console.WriteLine($"CRC32 created at address {allocLoc.ToString("X")}");
            //Console.ReadLine();
            
            IntPtr hKernelbase = Libloaderapi.GetModuleHandleA("KERNELBASE.dll");
            IntPtr isDebuggerPresentAddr = Libloaderapi.GetProcAddress(hKernelbase, "IsDebuggerPresent");


            
            while (true)
            {
                int c = 1;
                int crcVal = CRC32.AccumulateAtAddress(isDebuggerPresentAddr, 16);
              
                Console.WriteLine($"{c}: {crcVal} (IDP: {isDebuggerPresentAddr.ToString("X")}");
                Console.ReadLine();

            };

       
            //param passed to RCX
            uint checkMe = (uint)CRC32Check_Test(isDebuggerPresentAddr);

            while (true)
            {
                for (int i = 5; i > 0; i--)
                {
                    Console.WriteLine($"Next CRC32 Check in: {i}");
                    Thread.Sleep(1000);
                }


                int crcCheck = CRC32Check_Test(isDebuggerPresentAddr);
                int lolcheckme = 5;
            }
        }
        
        public static int CRC32Check_Test(IntPtr startAddr) => 
            ((TestDelegate)Marshal.GetDelegateForFunctionPointer(allocLoc, typeof(TestDelegate)))(startAddr);
    }
}
