﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IgroGadgets
{
    public class ProcessMemory
    {
        public const int DEFAULT_START_ADDRESS = 0x400000;

        enum ProcessAccessType
        {
            PROCESS_ALL_ACCESS = PROCESS_CREATE_PROCESS | PROCESS_CREATE_THREAD | PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION |
                                PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_SET_INFORMATION | PROCESS_SET_QUOTA | PROCESS_SUSPEND_RESUME |
                                PROCESS_TERMINATE | PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
            PROCESS_SET_INFORMATION = 0x0200,
            PROCESS_SET_QUOTA = 0x0100,
            PROCESS_SUSPEND_RESUME = 0x0800,
            PROCESS_TERMINATE = 0x0001,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020
        } 

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr dwSize, UIntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr dwSize, UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        private IntPtr _handleProcess;

        public ProcessMemory(string processName)
        {
            Process.EnterDebugMode();
            OpenProcessByName(processName);
        }

        public IntPtr OpenProcessByName(string processName)
        {
            Process process = Process.GetProcessesByName(processName)[0];
            _handleProcess = OpenProcess((int)ProcessAccessType.PROCESS_ALL_ACCESS, false, process.Id);
            return _handleProcess;
        }

        public bool ReadMemory(IntPtr readAddress, ref byte[] readBuffer)
        {
            return ReadProcessMemory(_handleProcess, readAddress, readBuffer, (UIntPtr)readBuffer.Length, UIntPtr.Zero);
        }

        public int ReadMemoryByte(IntPtr address)
        {
            int result = -1;
            var readBuffer = new byte[sizeof(byte)];
            if (ReadProcessMemory(_handleProcess, address, readBuffer, (UIntPtr)1, UIntPtr.Zero))
            {
                result = BitConverter.ToInt32(readBuffer, 0);
            }
            return result;
        }

        public IntPtr ReadMemoryIntPtr(IntPtr address)
        {   
            var result = IntPtr.Zero;
            var readBuffer = new byte[IntPtr.Size];
            if (ReadMemory(address, ref readBuffer))
            {
                Marshal.Copy(readBuffer, 0, result, readBuffer.Length);
            }
            return result;
        }

        public int ReadMemoryInt(IntPtr address)
        {
            var readInt = -1;
            var readBuffer = new byte[sizeof(int)];
            if (ReadMemory(address, ref readBuffer))
            {
                readInt = BitConverter.ToInt32(readBuffer, 0);
            }
            return readInt;
        }

        public long ReadMemoryLong(IntPtr address)
        {
            long result = -1;
            var readBuffer = new byte[sizeof(long)];
            if (ReadMemory(address, ref readBuffer))
            {
                result = BitConverter.ToInt64(readBuffer, 0);
            }
            return result;
        }

        public float ReadMemoryFloat(IntPtr address)
        {
            var readFloat = -1f;
            var readBuffer = new byte[IntPtr.Size];
            if (ReadMemory(address, ref readBuffer))
            {
                readFloat = BitConverter.ToSingle(readBuffer, 0);
            }
            return readFloat;
        }

        public bool WriteMemory(IntPtr writeAddress, byte[] writeBuffer)
        {
            return WriteProcessMemory(_handleProcess, writeAddress, writeBuffer, (UIntPtr)writeBuffer.Length, UIntPtr.Zero);
        }

        public bool WriteMemoryByte(IntPtr address, int value)
        {
            return value < 256 && WriteProcessMemory(_handleProcess, address, BitConverter.GetBytes(value), (UIntPtr)1, UIntPtr.Zero);
        }

        public bool WriteMemoryInt(IntPtr address, int value)
        {
            return WriteMemory(address, BitConverter.GetBytes(value));
        }

        public bool WriteMemoryLong(IntPtr address, long value)
        {
            return WriteMemory(address, BitConverter.GetBytes(value));
        }

        public bool WriteMemoryFloat(IntPtr address, float value)
        {
            return WriteMemory(address, BitConverter.GetBytes(value));
        }

        // The pointer offets array have to looks like this: { 0xFB3E3C, 0x60, 0x8,...}
        // First offset makes the base address: 0x400000 + 0xFB3E3C = 0x13B3E3C
        public IntPtr ReadPointer(IntPtr address, int[] offsets)
        {
            for (var i = 0; i < offsets.Length; i++)
            {
                if (i == offsets.Length - 1)
                {
                    address = IntPtr.Add(address, offsets[i]);
                }
                else
                {
                    address = ReadMemoryIntPtr(IntPtr.Add(address, offsets[i]));
                    if (address.Equals(IntPtr.Zero))
                    {
                        break;
                    }
                }
            }            
            return address;
        }

        public void Close()
        {
            Process.LeaveDebugMode();
            if(!_handleProcess.Equals(IntPtr.Zero))
            {
                CloseHandle(_handleProcess);
            }            
        }
    }
}
