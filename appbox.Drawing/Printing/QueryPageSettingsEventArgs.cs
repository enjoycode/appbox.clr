using System;

namespace appbox.Drawing.Printing
{

    /// <summary>
	/// Summary description for QueryPageSettingsEventHandler.
	/// </summary>
	public delegate void QueryPageSettingsEventHandler(object sender, QueryPageSettingsEventArgs e);

    /// <summary>
    /// Summary description for QueryPageSettingEventArgs.
    /// </summary>
    public class QueryPageSettingsEventArgs : PrintEventArgs
    {
        private PageSettings pageSettings;

        public QueryPageSettingsEventArgs(PageSettings pageSettings)
        {
            this.pageSettings = pageSettings;
        }
        public PageSettings PageSettings
        {
            get { return pageSettings; }
            set { pageSettings = value; }
        }

    }
}
