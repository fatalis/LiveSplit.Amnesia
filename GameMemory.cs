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
            // this is [49EDC2] as seen below
            //if (!p.WriteBytes(p.MainModule.BaseAddress + 0x9EDC2, 0))
            //    return false;
            if (!p.WriteBytes(p.MainModule.BaseAddress + 0x9DE12, 0))
                return false;

            // the following patches are in Amnesia.cLuxMapHandler::CheckMapChange(afTimeStep)
            // (the game kindly provides us with a .pdb)

            // overwrite useless code and set loading var to 1
            //
            // patch
            // 0049EE93      C74424 64 00000000         MOV     DWORD PTR SS:[ESP+64], 0
            // to
            // 0049EE93      C605 C2ED4900 01           MOV     BYTE PTR DS:[49EDC2], 1
            // 0049EE9A      90                         NOP
            //if (!p.WriteBytes(p.MainModule.BaseAddress + 0x9EE93, 0xC6, 0x05, 0xC2, 0xED, 0x49, 0x00, 0x01, 0x90))
            //    return false;
            if (!p.WriteBytes(p.MainModule.BaseAddress + 0x9DEE3, 0xC6, 0x05, 0x12, 0xDE, 0x49, 0x00, 0x01, 0x90))
                return false;

            // overwrite useless code and set loading var to 0
            //
            // patch
            // 0049F061      C64424 70 04               MOV     BYTE PTR SS:[ESP+70], 4
            // 0049F066      E8 9520F6FF                CALL    ProgLog
            // to
            // 0049F061      C605 C2ED4900 00           MOV     BYTE PTR DS:[49EDC2], 0
            // 0049F068      90                         NOP
            // 0049F069      90                         NOP
            // 0049F06A      90                         NOP
            //if (!p.WriteBytes(p.MainModule.BaseAddress + 0x9F062, 0x05, 0xC2, 0xED, 0x49, 0x00, 0x00, 0x90, 0x90, 0x90))
            //    return false;
            if (!p.WriteBytes(p.MainModule.BaseAddress + 0x9E0B2, 0x05, 0x12, 0xDE, 0x49, 0x00, 0x00, 0x90, 0x90, 0x90))
                return false;

            return true;
        }

        void HandleProcess(Process game, CancellationTokenSource cts)
        {
            bool prevIsLoading = false;

            while (!game.HasExited && !cts.IsCancellationRequested)
            {
                bool isLoading;
                //game.ReadBool(game.MainModule.BaseAddress + 0x9EDC2, out isLoading);
                game.ReadBool(game.MainModule.BaseAddress + 0x9DE12, out isLoading);

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
