﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LiveSplit.ComponentUtil;

namespace LiveSplit.Amnesia
{
    class GameMemory
    {
        public event EventHandler<LoadingChangedEventArgs> OnLoadingChanged;

        private const int UNUSED_BYTE_OFFSET = 0xC9858;

        private Process _process;
        private MemoryWatcher<bool> _isLoading;

        public void Update()
        {
            if (_process == null || _process.HasExited)
            {
                _process = null;
                if (!this.TryGetGameProcess())
                    return;
            }

            if (_isLoading.Update(_process))
                this.OnLoadingChanged?.Invoke(this, new LoadingChangedEventArgs(_isLoading.Current));
        }

        bool TryGetGameProcess()
        {
            Process p = Process.GetProcesses().FirstOrDefault(x => x.ProcessName.ToLower() == "amnesia");
            if (p == null || p.HasExited)
                return false;

            byte[] addrBytes = BitConverter.GetBytes((uint)p.MainModuleWow64Safe().BaseAddress + UNUSED_BYTE_OFFSET);

            // the following code has a very small chance to crash the game due to not suspending threads while writing memory
            // commented out stuff is for the cracked version of the game (easier to debug when there's no copy protection)

            // overwrite unused alignment byte with and initialize as our "is loading" var
            // this is [419858] as seen below
            if (!p.WriteBytes(p.MainModuleWow64Safe().BaseAddress + UNUSED_BYTE_OFFSET, 0))
                return false;

            // the following patches are in Amnesia.cLuxMapHandler::CheckMapChange(afTimeStep)
            // (the game kindly provides us with a .pdb)

            // overwrite useless code and set loading var to 1
            //
            // patch
            // 00419984      837D E8 10                 CMP     DWORD PTR SS:[EBP-18], 10
            // 00419988      C645 FC 00                 MOV     BYTE PTR SS:[EBP-4], 0
            // 0041998C      72 0C                      JB      SHORT 0041999A

            // to
            // 00419984      C605 58984100              MOV     BYTE PTR DS:[419858], 1
            // 0041998B      90                         NOP
            // 0041998C      EB 0C                      JMP     SHORT 0041999A
            var payload1 = new List<byte>(new byte[] { 0xC6, 0x05 });
            payload1.AddRange(addrBytes);
            payload1.AddRange(new byte[] { 0x01, 0x90, 0xEB });
            if (!p.WriteBytes(p.MainModuleWow64Safe().BaseAddress + 0xC9984, payload1.ToArray()))
                return false;

            // overwrite useless code and set loading var to 0
            //
            // patch
            // 00419AF9      C645 FC 04                 MOV     BYTE PTR SS:[EBP-4], 4
            // 00419AFD      E8 DE75F3FF                CALL    ProgLog
                                                        
            // to                                       
            // 00419AF9      C605 58984100              MOV     BYTE PTR DS:[419858], 0
            // 00419B00      90                         NOP
            // 00419B01      90                         NOP
            var payload2 = new List<byte>(new byte[] { 0x05 });
            payload2.AddRange(addrBytes);
            payload2.AddRange(new byte[] { 0x00, 0x90, 0x90 });
            if (!p.WriteBytes(p.MainModuleWow64Safe().BaseAddress + 0xC9AFA, payload2.ToArray()))
                return false;

            _isLoading = new MemoryWatcher<bool>(p.MainModuleWow64Safe().BaseAddress + UNUSED_BYTE_OFFSET);
            _process = p;

            return true;
        }
    }

    class LoadingChangedEventArgs : EventArgs
    {
        public bool IsLoading { get; private set; }

        public LoadingChangedEventArgs(bool isLoading)
        {
            this.IsLoading = isLoading;
        }
    }
}
