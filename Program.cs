using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CRC32
{
    class Program : NativeMethods
    {

        static IntPtr allocLoc;
        static readonly byte[] crcCheckCode = new byte[]
        {
            0x55,                                           //push rbp
            0x48, 0x89, 0xe5,                               //mov rbp, rsp,
            0x48, 0x8b, 0x49, 0x09,                         //mov rcx, [rcx+9]
            0xf2, 0x48, 0x0f, 0x38, 0xf1, 0xc1,             //crc32 rax, rcx
            0x48, 0x89, 0xec,                               //mov rsp, rbp
            0x5d,                                           //pop rbp
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

            
            IntPtr hKernelbase = Libloaderapi.GetModuleHandleA("KERNELBASE.dll");
            IntPtr isDebuggerPresentAddr = Libloaderapi.GetProcAddress(hKernelbase, "IsDebuggerPresent");


            Console.WriteLine($"IsDebuggerPresent Addresss = {isDebuggerPresentAddr.ToString("X")}\n");

       
            //param passed to RCX
            CRC32Check(isDebuggerPresentAddr);


            while (true)
            {
                for (int i = 5; i > 0; i--)
                {
                    Console.WriteLine($"Next CRC32 Check in: {i}");
                    Thread.Sleep(1000);
                }


                CRC32Check(isDebuggerPresentAddr);
                Thread.Sleep(1000);

            }
        }
        public static void CRC32Check(IntPtr isDebuggerPresentAddr)
        {
            IntPtr thread = Processthreadsapi.CreateRemoteThread(Process.GetCurrentProcess().Handle, IntPtr.Zero, 0, allocLoc, isDebuggerPresentAddr, 0, out IntPtr threadId);
            Thread.Sleep(100);

            if (!Processthreadsapi.GetExitCodeThread(thread, out uint exitCode))
                throw new Exception($"GetExitCodeThread failed with System Error Code {Marshal.GetLastWin32Error()}");

            if (exitCode == 259)
            {
                Console.WriteLine("Waiting for CRC Thread to finish.");
                for (int i = 1; i <= 10; i++)
                {
                    Processthreadsapi.GetExitCodeThread(thread, out exitCode);
                    if (exitCode != 259)
                        break;

                    if (i == 10)
                        throw new Exception($"10 seconds have passed and the thread is still running. Something is wrong. Last recorded error code: {Marshal.GetLastWin32Error()}");

                    Thread.Sleep(1000);
                }
            }
            Console.WriteLine($"CRC32 value of {isDebuggerPresentAddr.ToString("X")} + 9 is {exitCode}\n");
            Thread.Sleep(1000);
        }
    }
}
