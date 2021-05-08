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

        /// <summary>
        /// Creates and executes a CRC32 check dynamically. Memory is immediately erased post check.
        /// This is more secure, but induces more overhead.
        /// </summary>
        /// <param name="address">The initial starting address for the CRC32 check.</param>
        /// <param name="crcSize">The length of the check. Must be greater than 0.</param>
        /// <param name="crcValue">The produced value of the CRC32 check.</param>
        /// <returns></returns>
        public static ErrorCodes DynamicAccumulateAtAddress(IntPtr address, uint crcSize, out int crcValue)
        {
            crcValue = default;
            Dictionary<Registers, int> requiredRegisters = new()
            {
                { Registers.RAX, 1 },
                { Registers.EAX, 0 },
                { Registers.AX, 0 },
                { Registers.AL, 0 },
            };

            if (crcSize != 8)
                requiredRegisters = CalculatorRegisterCount((int)crcSize);

            byte[] assembly = GenerateAssembly(requiredRegisters);
            IntPtr allocLoc = Memoryapi.VirtualAlloc(IntPtr.Zero, (uint)assembly.Length, Winnt.AllocationType.MEM_COMMIT, Winnt.MemoryProtection.PAGE_EXECUTE_READWRITE);

            try
            {
                Memoryapi.WriteProcessMemory(Process.GetCurrentProcess().Handle, allocLoc, assembly, assembly.Length, out IntPtr _);
            }
            catch
            {
                return ErrorCodes.WRITEPROCESSMEMORY_FAILED;
            }

#if DEBUG
            Console.WriteLine($"CRC Check Location: {allocLoc:X}");
            //Console.Read();
#endif
            crcValue = ((crcFunctionDelegate)Marshal.GetDelegateForFunctionPointer
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

            //Zero the memory out, as VirtualFree doesn't guarantee this.
            Wdm.ZeroMemory(allocLoc, (IntPtr)assembly.Length);

            if (!Memoryapi.VirtualFree(allocLoc, 0, 0x00008000))
                return ErrorCodes.VIRTUALFREE_FAILED; //MEM_RELEASE - LAZY! fix.

            return ErrorCodes.NO_ERROR;
        }
        private static Dictionary<Registers, int> CalculatorRegisterCount(int x)
        {
            List<byte> combinations = new();
            Dictionary<Registers, int> registerCount = new()
            {
                { Registers.RAX, 0 },
                { Registers.EAX, 0 },
                { Registers.AX, 0 },
                { Registers.AL, 0 }
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
        private static byte[] GenerateAssembly(Dictionary<Registers, int> registers)
        {
            List<byte> crcInstructions = new()
            {
                0x48,
                0x31,
                0xc0    //xor rax, rax
            };
            if (registers[Registers.RAX] != 0)
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
            if (registers[Registers.EAX] != 0)
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
            if (registers[Registers.AX] != 0)
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
            if (registers[Registers.AL] != 0)
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

            #region crcepilogue
            //RAX will contain CRC value.
            crcInstructions.Add(0xC3);  //ret
            #endregion

            return crcInstructions.ToArray();
        }
    private enum Registers
        {
            RAX,
            EAX,
            AX,
            AL
        }
        public enum ErrorCodes
        {
            NO_ERROR,
            CRC_SIZE_TOO_SMALL,
            VIRTUALFREE_FAILED,
            WRITEPROCESSMEMORY_FAILED,
        }
    }
}
