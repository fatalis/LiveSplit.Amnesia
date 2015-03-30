using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.Amnesia
{
    class GameMemory
    {
        public event EventHandler<LoadingChangedEventArgs> OnLoadingChanged; 

        private Task _thread;
        private SynchronizationContext _uiThread;
        private CancellationTokenSource _cancelSource;

        public void StartReading()
        {
            if (_thread != null && _thread.Status == TaskStatus.Running)
                throw new InvalidOperationException();
            if (!(SynchronizationContext.Current is WindowsFormsSynchronizationContext))
                throw new InvalidOperationException("SynchronizationContext.Current is not a UI thread.");

            _cancelSource = new CancellationTokenSource();
            _uiThread = SynchronizationContext.Current;
            _thread = Task.Factory.StartNew(() => MemoryReadThread(_cancelSource));
        }

        public void Stop()
        {
            if (_cancelSource == null || _thread == null || _thread.Status != TaskStatus.Running)
                return;

            _cancelSource.Cancel();
            _thread.Wait();
        }

        void MemoryReadThread(CancellationTokenSource cts)
        {
            while (true)
            {
                try
                {
                    Process game;
                    while (!this.TryGetGameProcess(out game))
                    {
                        Thread.Sleep(500);

                        if (cts.IsCancellationRequested)
                            goto ret;
                    }

                    this.HandleProcess(game, cts);

                    if (cts.IsCancellationRequested)
                        goto ret;
                }
                catch (Exception ex) // probably a Win32Exception on access denied to a process
                {
                    Trace.WriteLine(ex.ToString());
                    Thread.Sleep(1000);
                }
            }

        ret: ;
        }

        bool TryGetGameProcess(out Process p)
        {
            p = Process.GetProcesses().FirstOrDefault(x => x.ProcessName.ToLower() == "amnesia");
            if (p == null || p.HasExited)
                return false;

            // the following code has a very small chance to crash the game due to not suspending threads while writing memory
            // commented out stuff is for the cracked version of the game (easier to debug when there's no copy protection)

            // overwrite unused alignment byte with and initialize as our "is loading" var
            // this is [419858] as seen below
            if (!p.WriteBytes(p.MainModule.BaseAddress + 0xC9858, 0))
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
            if (!p.WriteBytes(p.MainModule.BaseAddress + 0xC9984, 0xC6, 0x05, 0x58, 0x98, 0x41, 0x00, 0x01, 0x90, 0xEB))
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
            if (!p.WriteBytes(p.MainModule.BaseAddress + 0xC9AFA, 0x05, 0x58, 0x98, 0x41, 0x00, 0x00, 0x90, 0x90))
                return false;

            return true;
        }

        void HandleProcess(Process game, CancellationTokenSource cts)
        {
            bool prevIsLoading = false;

            while (!game.HasExited && !cts.IsCancellationRequested)
            {
                bool isLoading;
                game.ReadBool(game.MainModule.BaseAddress + 0xC9858, out isLoading);

                if (isLoading != prevIsLoading)
                {
                    _uiThread.Post(d => {
                        if (this.OnLoadingChanged != null)
                            this.OnLoadingChanged(this, new LoadingChangedEventArgs(isLoading));
                    }, null);
                }

                prevIsLoading = isLoading;

                Thread.Sleep(15);
            }
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
