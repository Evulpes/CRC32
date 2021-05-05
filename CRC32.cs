using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace CRC32
{
    class CRC32 : NativeMethods
    {
        delegate int crcFunctionDelegate(IntPtr addr, int size);

        public static long AccumulateAtAddress(IntPtr address, uint crcSize=8)
        {
            if (crcSize < 8)
                return long.MaxValue;

            int eightRemain = (int)crcSize % 8;
            int eightMultiple = (int)crcSize / 8;

            List<byte> crcInstructions = new List<byte>()
            {
                0x48, 0x31, 0xc0    //xor rax, rax
            };
            if (crcSize > 8)
            {
                crcInstructions.AddRange
                (
                    new byte[] { 0x49, 0x89, 0xd1 } //mov r9, rdx
                );
            }


            crcInstructions.AddRange
            (
                new byte[]
                {
                    0x4c, 0x8b, 0x19,                   //mov r11 [rcx]
                    0xf2, 0x49, 0x0f, 0x38, 0xf1, 0xc3  //crc rax, r11
                }
            );

            if (crcSize > 8)
            {
                crcInstructions.AddRange
                (
                    new byte[]
                    {
                        0x48, 0x83, 0xc1, 0x08, //add rcx, 8
                        0x49, 0xff, 0xc9,       //dec r9
                        0x49, 0x83, 0xf9, 0x00, //cmp r9, 0
                        0x75, 0xea              //jne -0x17
                        
                    }
                );
            }
            crcInstructions.Add(0xC3);  //ret
            byte[] assembly = crcInstructions.ToArray();

            IntPtr allocLoc = Memoryapi.VirtualAlloc(IntPtr.Zero, (uint)assembly.Length, Winnt.AllocationType.MEM_COMMIT, Winnt.MemoryProtection.PAGE_EXECUTE_READWRITE);
            Memoryapi.WriteProcessMemory(Process.GetCurrentProcess().Handle, allocLoc, assembly, assembly.Length, out IntPtr _);

#if DEBUG
            Console.WriteLine($"Loc: {allocLoc.ToString("X")}");
            Console.Read();
#endif            
            int result = ((crcFunctionDelegate)Marshal.GetDelegateForFunctionPointer(allocLoc, typeof(crcFunctionDelegate)))(address, eightMultiple);

            Memoryapi.VirtualFree(allocLoc, 0, 0x00004000); //MEM_DECOMMIT - LAZY! fix.

            return result;
        }
    }
}
