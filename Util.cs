using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LiveSplit.Amnesia
{
    public static class Extensions
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            int dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);

        public static bool WriteBytes(this Process process, IntPtr addr, params byte[] bytes)
        {
            const uint PAGE_EXECUTE_READWRITE = 0x40;

            uint oldProtect;
            if (!VirtualProtectEx(process.Handle, addr, bytes.Length,
                PAGE_EXECUTE_READWRITE, out oldProtect))
                return false;

            int written;
            if (!WriteProcessMemory(process.Handle, addr, bytes, bytes.Length, out written)
                || written != bytes.Length)
                return false;

            // SafeNativeMethods.VirtualProtectEx(process.Handle, addr, bytes.Length, oldProtect, out oldProtect);

            return true;
        }
    }
}
