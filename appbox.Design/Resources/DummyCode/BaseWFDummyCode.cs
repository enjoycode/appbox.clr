using System;

namespace wf
{
    
    public abstract class WorkflowInstance
    {
        //public System.Guid ID {get {return System.Guid.Empty;}}

        public string Title {get;set;}

        public void Start() { }

        //public void StartAsync(System.Action<string> callback) { }
    }

    public abstract class Activity
    {

    }

    public abstract class HumanActivity : Activity
    {

    }

    public sealed class SingleHumanActivity : HumanActivity
    {
        public string Result { get; }
    }

    public sealed class MultiHumanActivity : HumanActivity
    {
        public int HumanCount { get { return 0; } }
    }

    //public class ActivityEventArgs<TInstance, TActiviey> : EventArgs where TInstance : WorkflowInstance where TActiviey : Activity
    //{
    //    public TInstance Instance { get { return null; } }
    //    public TActiviey Activity { get { return null; } }
    //}

    public sealed class ShowViewEventArgs<TInstance> : System.EventArgs where TInstance : WorkflowInstance
    {

        public TInstance Instance {get { return null; }}

        public HumanActivity Activity {get { return null; }}

        public ui.RootView View {get;set;}

        //public HumanActionInfo ActionInfo { get { return null; } } or Bookmark
    }

}
