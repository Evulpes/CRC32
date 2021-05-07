using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
namespace CRC32
{
    class CRC32 : NativeMethods
    {
        delegate int crcFunctionDelegate(IntPtr addr, int[] registerLoops /*int size*/);

        public static int DynamicAccumulateAtAddress(IntPtr address, uint crcSize)
        {
            Dictionary<Registers, int> requiredRegisters = new()
            {
                { Registers.RAX, 1 },
                { Registers.EAX, 0 },
                { Registers.AX, 0 },
                { Registers.AL, 0 },
            };
            #region crcprologue
            List<byte> crcInstructions = new ()
            {
                0x48, 0x31, 0xc0    //xor rax, rax
            };
            #endregion
            if (crcSize != 8)
            {
                requiredRegisters = CalculatorRegisterCount((int)crcSize);
                if (requiredRegisters[Registers.RAX] != 0)
                {

                    crcInstructions.AddRange
                    (
                        new byte[] 
                        { 
                            0x44, 0x8b, 0x0a,                   //mov r9d, [rdx]
                            0x4c, 0x8b, 0x19,                   //mov r11 [rcx]
                            0xf2, 0x49, 0x0f, 0x38, 0xf1, 0xc3, //crc rax, r11
                            0x48, 0x83, 0xc1, 0x08,             //add rcx, 8
                            0x49, 0xff, 0xc9,                   //dec r9
                            0x49, 0x83, 0xf9, 0x00,             //cmp r9, 0
                            0x75, 0xea                          //jne -0x14
                        } 
                    );

                }
                if (requiredRegisters[Registers.EAX] != 0)
                {
                    crcInstructions.AddRange
                    (
                        new byte[]
                        {
                            0x44, 0x8b, 0x4a, 0x04,             //mov r9d, [rdx+4]
                            0x4d, 0x31, 0xdb,                   //xor r11, r11
                            0x44, 0x8b, 0x19,                   //mov r11d [rcx]
                            0xf2, 0x49, 0x0f, 0x38, 0xf1, 0xc3, //crc rax, r11
                            0x48, 0x83, 0xc1, 0x04,             //add rcx, 4
                            0x49, 0xff, 0xc9,                   //dec r9
                            0x49, 0x83, 0xf9, 0x00,             //cmp r9, 0
                            0x75, 0xeb                          //jne -0x14
                        }
                    );
                }
                if (requiredRegisters[Registers.AX] != 0)
                {
                    crcInstructions.AddRange
                    (
                        new byte[]
                        {
                            0x44, 0x8b, 0x4a, 0x08,             //mov r9d, [rdx+8]
                            0x4d, 0x31, 0xdb,                   //xor r11, r11
                            0x66, 0x44, 0x8b, 0x19,             //mov r11w, [rcx]
                            0xf2, 0x49, 0x0f, 0x38, 0xf1, 0xc3, //crc rax, r11
                            0x48, 0x83, 0xc1, 0x02,             //add rcx, 2
                            0x49, 0xff, 0xc9,                   //dec r9
                            0x49, 0x83, 0xf9, 0x00,             //cmp r9, 0
                            0x75, 0xeb                          //jne -0x13
                        }
                    );
                }
                if (requiredRegisters[Registers.AL] != 0)
                {
                    crcInstructions.AddRange
                    (
                        new byte[]
                        {
                            0x44, 0x8b, 0x4a, 0x0C,             //mov r9d, [rdx+0x12]
                            0x4d, 0x31, 0xdb,                   //xor r11, r11
                            0x44, 0x8a, 0x19,                   //mov r11b, [rcx]
                            0xf2, 0x49, 0x0f, 0x38, 0xf1, 0xc3, //crc rax, r11
                            0x48, 0xff, 0xc1,                   //inc rcx
                            0x49, 0xff, 0xc9,                   //dec r9
                            0x49, 0x83, 0xf9, 0x00,             //cmp r9, 0
                            0x75, 0xeb                          //jne -0x13
                        }
                    );
                }

            }




            int eightRemain = (int)crcSize % 8;
            int eightMultiple = (int)crcSize / 8;

            #region crcepilogue
            crcInstructions.Add(0xC3);  //ret
            #endregion

            byte[] assembly = crcInstructions.ToArray();

            IntPtr allocLoc = Memoryapi.VirtualAlloc(IntPtr.Zero, (uint)assembly.Length, Winnt.AllocationType.MEM_COMMIT, Winnt.MemoryProtection.PAGE_EXECUTE_READWRITE);
            Memoryapi.WriteProcessMemory(Process.GetCurrentProcess().Handle, allocLoc, assembly, assembly.Length, out IntPtr _);


            Console.WriteLine($"LocDB: {allocLoc:X}");
            Console.Read();
     
            int result = ((crcFunctionDelegate)Marshal.GetDelegateForFunctionPointer
            (
                allocLoc, 
                typeof(crcFunctionDelegate))
            )
            (
                address, 
                new int[] 
                { 
                    requiredRegisters[Registers.RAX], 
                    requiredRegisters[Registers.EAX], 
                    requiredRegisters[Registers.AX], 
                    requiredRegisters[Registers.AL] 
                }
            );

            bool testme = Memoryapi.VirtualFree(allocLoc, 0, 0x00004000); //MEM_DECOMMIT - LAZY! fix.

            return result;
        }
        private static Dictionary<Registers, int> CalculatorRegisterCount(int x)
        {
            List<byte> combinations = new();
            Dictionary<Registers, int> registerCount = new()
            {
                { Registers.RAX, 0 },
                { Registers.EAX, 0 },
                { Registers.AX, 0},
                { Registers.AL, 0}
            };
            byte[] values = new byte[]
            {
                8, 4, 2, 1
            };
            return RecursiveRegisterAccumulator(x, out _);

            Dictionary<Registers, int> RecursiveRegisterAccumulator(int x, out int i)
            {
                for (i = 0; i < values.Length; i++)
                {
                    int remain = x % values[i];
                    int multiple = x / values[i];

                    if (multiple == 0)
                        continue;

                    if (multiple >= 1)
                    {
                        switch (values[i])
                        {
                            case 8:
                                registerCount[Registers.RAX] += multiple;
                                break;
                            case 4:
                                registerCount[Registers.EAX] += multiple;
                                break;
                            case 2:
                                registerCount[Registers.AX] += multiple;
                                break;
                            case 1:
                                registerCount[Registers.AL] += multiple;
                                break;
                        }
                    }

                    if (remain == 1 && multiple == 0)
                        registerCount[Registers.AL]++;
                    else if (remain >= 1)
                        RecursiveRegisterAccumulator(remain, out i);
                    
                    if (remain == 0)
                    {
                        i = 4;
                        return registerCount;
                    }

                }
                return registerCount;
            }
        }
        enum Registers
        {
            RAX,
            EAX,
            AX,
            AL
        }
    }
}
