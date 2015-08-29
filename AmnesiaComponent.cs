using System;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;

namespace LiveSplit.Amnesia
{
    class AmnesiaComponent : LogicComponent
    {
        public override string ComponentName => "Amnesia";

        private GameMemory _gameMemory;
        private TimerModel _timer;

        public AmnesiaComponent(LiveSplitState state)
        {
            _timer = new TimerModel() { CurrentState = state };
            _timer.OnStart += timer_OnStart;

            _gameMemory = new GameMemory();
            _gameMemory.OnLoadingChanged += gameMemory_OnLoadingChanged;
            _gameMemory.StartReading();
        }

        public override void Dispose()
        {
            _timer.OnStart -= timer_OnStart;
            _gameMemory?.Stop();
        }

        void timer_OnStart(object sender, EventArgs eventArgs)
        {
            _timer.InitializeGameTime();
        }

        void gameMemory_OnLoadingChanged(object sender, LoadingChangedEventArgs e)
        {
            _timer.CurrentState.IsGameTimePaused = e.IsLoading;
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return null;
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            
        }


        public override void SetSettings(XmlNode settings)
        {
           
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return document.CreateElement("Settings");
        }
    }
}
