﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace HMMCodes
{
    public static unsafe class MemoryService
    {
        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtect(IntPtr lpAddress,
                IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
        
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);
        
        public static dynamic MemoryProvider;

        public static void RegisterProvider(object provider)
        {
            MemoryProvider = provider;
        }

        public static void Write(IntPtr address, IntPtr dataPtr, IntPtr length)
            => MemoryProvider.WriteMemory(address, dataPtr, length);

        public static void Write<T>(IntPtr address, T data)
            => MemoryProvider.WriteMemory<T>(address, data);
        
        public static void Write<T>(long address, params T[] data)
            => MemoryProvider.WriteMemory<T>((IntPtr)address, data);
        
        public static void Write<T>(long address, T data)
            => Write<T>((IntPtr)address, data);
        
        public static char[] Read(IntPtr address, IntPtr length)
            => MemoryProvider.ReadMemory(address, length);
        
        public static T Read<T>(IntPtr address) where T : unmanaged
            => MemoryProvider.ReadMemory<T>(address);
            
        public static T Read<T>(long address) where T : unmanaged
            => Read<T>((IntPtr)address);
            
        public static byte[] Assemble(string source)
            => MemoryProvider.AssembleInstructions(source);
        
        public static long GetPointer(long address, params long[] offsets)
        {
            if(address == 0)
                return 0;
            
            var result = (long)(*(void**)address);
            
            if(result == 0)
                return 0;
            
            if(offsets.Length > 0)
            {
                for(int i = 0; i < offsets.Length - 1; i++)
                {
                    result = (long)((void *)(result + offsets[i]));
                    result = (long)(*(void **)result);
                    if (result == 0)
                        return 0;
                }
                
                return result + offsets[offsets.Length - 1];
            }
            
            return result;
        }
        
        public static void WriteProtected(IntPtr address, IntPtr dataPtr, IntPtr length)
        {
            VirtualProtect((IntPtr)address, length, 0x04, out uint oldProtect);
            Write(address, dataPtr, length);
            VirtualProtect((IntPtr)address, length, oldProtect, out _);
        }
        
        public static void WriteProtected<T>(long address, T data) where T : unmanaged
        {
            VirtualProtect((IntPtr)address, (IntPtr)sizeof(T), 0x04, out uint oldProtect);
            Write<T>(address, data);
            VirtualProtect((IntPtr)address, (IntPtr)sizeof(T), oldProtect, out _);
        }
        
        public static void WriteProtected<T>(long address, params T[] data) where T : unmanaged
        {
            VirtualProtect((IntPtr)address, (IntPtr)(sizeof(T) * data.Length), 0x04, out uint oldProtect);
            Write<T>(address, data);
            VirtualProtect((IntPtr)address, (IntPtr)(sizeof(T) * data.Length), oldProtect, out _);
        }
        
        public static void WriteAsmHook(string instructions, long address, HookBehavior behavior = HookBehavior.After)
            => MemoryProvider.WriteASMHook(instructions, (IntPtr)address, (int)behavior);
        
        public static void WriteAsmHook(long address, HookBehavior behavior, params string[] instructions)
            => WriteAsmHook(string.Join("\r\n", instructions), address, behavior);
        
        public static void WriteNop(long address, long count)
        {
            for (long i = 0; i < count; i++)
            {
                WriteProtected<byte>(address + i, 0x90);
            }
        }
        
        public static bool IsKeyDown(Keys key)
            => (GetAsyncKeyState(key) & 1) == 1;
    }
    
    public enum HookBehavior
	{
		After,
		Before,
		Replace
	}
}