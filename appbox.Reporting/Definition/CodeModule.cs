
using System;
using System.Xml;
using System.Reflection;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// CodeModule definition and processing.
	///</summary>
	[Serializable]
	internal class CodeModule : ReportLink
	{
		string _CodeModule;	// Name of the code module to load
		[NonSerialized] Assembly _LoadedAssembly=null;	// 
		[NonSerialized] bool bLoadFailed=false;
	
		internal CodeModule(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_CodeModule=xNode.InnerText;
            //Added from Forums, User: Solidstore
            if (!_CodeModule.Contains(","))
            { // if not a full assembly reference 
                if ((!_CodeModule.ToLower().EndsWith(".dll")) && ((!_CodeModule.ToLower().EndsWith(".exe"))))
                { // check .dll ending 
                _CodeModule += ".dll"; 
                }
            }
		}

		internal Assembly LoadedAssembly()
		{
			if (bLoadFailed)		// We only try to load once.
				return null;

			if (_LoadedAssembly == null)
			{
				try
				{
					_LoadedAssembly = XmlUtil.AssemblyLoadFrom(_CodeModule);
				}
				catch (Exception e)
				{
					OwnerReport.rl.LogError(4, String.Format("CodeModule {0} failed to load.  {1}",
						_CodeModule, e.Message));
					bLoadFailed = true;
				}
			}
			return _LoadedAssembly;
		}

		override internal void FinalPass()
		{
			return;
		}

		internal string CdModule
		{
			get { return  _CodeModule; }
			set {  _CodeModule = value; }
		}
	}
}
