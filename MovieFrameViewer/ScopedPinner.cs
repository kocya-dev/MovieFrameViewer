using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MovieFrameViewer
{
    public class ScopedPinner : IDisposable
    {
        private GCHandle _pinnedArray;
        public ScopedPinner(byte[] array)
        {
            _pinnedArray = GCHandle.Alloc(array, GCHandleType.Pinned);
        }
        public IntPtr GetPtr() => _pinnedArray.AddrOfPinnedObject();
        public void Dispose() => _pinnedArray.Free();
    }
}
