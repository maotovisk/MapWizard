using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MapWizard.Desktop.Utils;

/// <summary>
/// Allocates raw OS pages (VirtualAlloc / mmap) that live outside the GC heap.
/// </summary>
public static class PageAllocator
{
    /// <summary>
    /// Reserves and commits <paramref name="size"/> bytes of zeroed, read/write memory.
    /// </summary>
    public static IntPtr Alloc(nuint size)
    {
        if (OperatingSystem.IsWindows())
            return PageAllocatorWindows.Alloc(size);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return PageAllocatorUNIX.Alloc(size);

        throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// Releases a block previously returned by <see cref="Alloc"/>.
    /// </summary>
    public static void Free(IntPtr address, nuint size)
    {
        if (address == IntPtr.Zero)
            return;

        if (OperatingSystem.IsWindows())
            PageAllocatorWindows.Free(address);
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            PageAllocatorUNIX.Free(address, size);
        else
            throw new PlatformNotSupportedException();
    }
}

[SupportedOSPlatform("windows")]
internal static partial class PageAllocatorWindows
{
    // MEM_LARGE_PAGES is deliberately not used: it requires SeLockMemoryPrivilege and a size that is a
    // multiple of GetLargePageMinimum() (2 MiB on x64), so VirtualAlloc would fail for sub-2 MiB pages.
    [Flags]
    internal enum AllocationType
    {
        MEM_COMMIT = 0x00001000,
        MEM_RESERVE = 0x00002000,
    }

    internal enum ProtectionType
    {
        PAGE_READWRITE = 0x04,
    }

    internal enum FreeType
    {
        // MEM_RELEASE must be passed alone and with dwSize == 0.
        MEM_RELEASE = 0x00008000,
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr VirtualAlloc(
        IntPtr lpAddress,
        nuint dwSize,
        AllocationType flAllocationType,
        ProtectionType flProtect);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualFree(
        IntPtr lpAddress,
        nuint dwSize,
        FreeType dwFreeType);

    public static IntPtr Alloc(nuint size)
    {
        var address = VirtualAlloc(0, size, AllocationType.MEM_COMMIT | AllocationType.MEM_RESERVE, ProtectionType.PAGE_READWRITE);
        if (address == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), $"VirtualAlloc failed for {size} bytes");

        return address;
    }

    public static void Free(IntPtr address)
    {
        if (!VirtualFree(address, 0, FreeType.MEM_RELEASE))
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "VirtualFree failed");
    }
}

internal static partial class PageAllocatorUNIX
{
    private const int ProtRead = 0x1;
    private const int ProtWrite = 0x2;
    private const int MapPrivate = 0x02;

    // MAP_ANONYMOUS: 0x20 on Linux, but 0x1000 on macOS/BSD.
    private static readonly int MapAnon = OperatingSystem.IsMacOS() ? 0x1000 : 0x20;

    [LibraryImport("libc", SetLastError = true)]
    private static partial IntPtr mmap(
        IntPtr addr,
        nuint length,
        int prot,
        int flags,
        int fd,
        long offset);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int munmap(IntPtr addr, nuint length);

    public static IntPtr Alloc(nuint size)
    {
        var address = mmap(0, size, ProtRead | ProtWrite, MapPrivate | MapAnon, -1, 0);
        if (address == new IntPtr(-1))
            throw new Win32Exception(Marshal.GetLastPInvokeError(), $"mmap failed for {size} bytes");

        return address;
    }

    public static void Free(IntPtr address, nuint size)
    {
        if (munmap(address, size) != 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "munmap failed");
    }
}
