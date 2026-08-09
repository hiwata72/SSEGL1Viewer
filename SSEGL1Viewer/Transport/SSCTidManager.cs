using System;
using System.Collections.Generic;

namespace SSEGL1Viewer.Transport
{
    public sealed class SSCTidManager
    {
        private readonly object _syncRoot = new();

        private readonly HashSet<byte> _pendingTids = new();

        private byte _nextTid = 1;

        public byte Acquire()
        {
            lock (_syncRoot)
            {
                for (int i = 0; i < 15; i++)
                {
                    byte tid = _nextTid;

                    _nextTid++;

                    if (_nextTid > 15)
                    {
                        _nextTid = 1;
                    }

                    if (_pendingTids.Add(tid))
                    {
                        return tid;
                    }
                }

                throw new InvalidOperationException(
                    "利用可能なSSC TIDがありません。");
            }
        }

        public bool IsPending(byte tid)
        {
            tid &= 0x0F;

            lock (_syncRoot)
            {
                return _pendingTids.Contains(tid);
            }
        }

        public bool Complete(byte tid)
        {
            tid &= 0x0F;

            lock (_syncRoot)
            {
                return _pendingTids.Remove(tid);
            }
        }

        public void Cancel(byte tid)
        {
            Complete(tid);
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _pendingTids.Clear();
                _nextTid = 1;
            }
        }
    }
}