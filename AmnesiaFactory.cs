using System.Reflection;
using LiveSplit.Amnesia;
using LiveSplit.UI.Components;
using System;
using LiveSplit.Model;

[assembly: ComponentFactory(typeof(AmnesiaFactory))]

namespace LiveSplit.Amnesia
{
    public class AmnesiaFactory : IComponentFactory
    {
        public string ComponentName => "Amnesia";
        public string Description => "Load time remover for Amnesia: The Dark Descent";
        public ComponentCategory Category => ComponentCategory.Control;

        public IComponent Create(LiveSplitState state)
        {
            return new AmnesiaComponent(state);
        }

        public string UpdateName => this.ComponentName;
        public string UpdateURL => "http://fatalis.pw/livesplit/update/";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public string XMLURL => this.UpdateURL + "Components/update.LiveSplit.Amnesia.xml";
    }
}
