using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data
{
    internal sealed class HGlobalSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public HGlobalSafeHandle(IntPtr buffer)
            : base(ownsHandle: true)
        {
            SetHandle(buffer);
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
