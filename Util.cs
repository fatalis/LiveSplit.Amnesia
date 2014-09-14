using System;
using System.Diagnostics;

namespace LiveSplit.Amnesia
{
    public static class Extensions
    {
        public static bool ReadBytes(this Process process, IntPtr addr, byte[] bytes)
        {
            int read;
            if (!SafeNativeMethods.ReadProcessMemory(process.Handle, addr, bytes, bytes.Length, out read)
                || read != bytes.Length)
                return false;
            return true;
        }

        public static bool ReadBool(this Process process, IntPtr addr, out bool b)
        {
            var bytes = new byte[1];
            int read;
            b = false;
            if (!SafeNativeMethods.ReadProcessMemory(process.Handle, addr, bytes, bytes.Length, out read)
                || read != bytes.Length)
                return false;
            b = bytes[0] != 0;
            return true;
        }

        public static bool WriteBytes(this Process process, IntPtr addr, params byte[] bytes)
        {
            const uint PAGE_EXECUTE_READWRITE = 0x40;

            uint oldProtect;
            if (!SafeNativeMethods.VirtualProtectEx(process.Handle, addr, bytes.Length,
                PAGE_EXECUTE_READWRITE, out oldProtect))
                return false;

            int written;
            if (!SafeNativeMethods.WriteProcessMemory(process.Handle, addr, bytes, bytes.Length, out written)
                || written != bytes.Length)
                return false;

            // SafeNativeMethods.VirtualProtectEx(process.Handle, addr, bytes.Length, oldProtect, out oldProtect);

            return true;
        }
    }
}
