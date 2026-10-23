# Native snapshot avoids waiting on the WMI performance provider.
if(-not ('WorkBatch.PerformanceMemory' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
namespace WorkBatch {
    public static class PerformanceMemory {
        [StructLayout(LayoutKind.Sequential)]
        private struct Info {
            public uint Size;
            public UIntPtr CommitTotal, CommitLimit, CommitPeak;
            public UIntPtr PhysicalTotal, PhysicalAvailable, SystemCache;
            public UIntPtr KernelTotal, KernelPaged, KernelNonpaged, PageSize;
            public uint HandleCount, ProcessCount, ThreadCount;
        }
        [DllImport("psapi.dll", SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPerformanceInfo(ref Info info, uint size);
        public static double[] Read() {
            var info = new Info { Size = (uint)Marshal.SizeOf<Info>() };
            if (!GetPerformanceInfo(ref info, info.Size))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            double page = info.PageSize.ToUInt64();
            if (page <= 0 || info.CommitLimit.ToUInt64() == 0)
                throw new InvalidOperationException("Invalid native memory counters");
            return new[] { info.PhysicalAvailable.ToUInt64()*page,
                info.CommitTotal.ToUInt64()*page, info.CommitLimit.ToUInt64()*page };
        }
    }
}
'@
}
function Get-WorkMemory {
    $values=[WorkBatch.PerformanceMemory]::Read()
    [pscustomobject]@{AvailableMBytes=$values[0]/1MB;CommittedBytes=$values[1];CommitLimit=$values[2]}
}
