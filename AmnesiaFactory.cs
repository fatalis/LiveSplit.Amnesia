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
        public string ComponentName
        {
            get { return "Amnesia"; }
        }

        public string Description
        {
            get { return "Load time remover for Amnesia: The Dark Descent"; }
        }

        public ComponentCategory Category
        {
            get { return ComponentCategory.Control; }
        }

        public IComponent Create(LiveSplitState state)
        {
            return new AmnesiaComponent(state);
        }

        public string UpdateName
        {
            get { return this.ComponentName; }
        }

        public string UpdateURL
        {
            get { return "http://fatalis.pw/livesplit/update/"; }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public string XMLURL
        {
            get { return this.UpdateURL + "Components/update.LiveSplit.Amnesia.xml"; }
        }
    }
}
