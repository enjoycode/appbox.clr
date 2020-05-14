using System;

namespace appbox.Drawing.Printing
{
	/// <summary>
	/// Summary description for PaperSource.
	/// </summary>
	[Serializable]
	public class PaperSource
	{
		private PaperSourceKind kind;
		private string source_name;
		internal bool is_default;
		

		public PaperSource ()
		{
			
		}

		internal PaperSource(string sourceName, PaperSourceKind kind)
		{
			this.source_name = sourceName;
			this.kind = kind;
		}

		internal PaperSource(string sourceName, PaperSourceKind kind, bool isDefault)
		{
			this.source_name = sourceName;
			this.kind = kind;
			this.is_default = IsDefault;
		}

		public PaperSourceKind Kind{
			get {
				// Exactly at 256 (as opposed to Custom, which is 257 and the max value of PaperSourceKind),
				// we must return Custom always.
				if ((int)kind >= 256)
					return PaperSourceKind.Custom;

				return this.kind; 
			}
		}
		public string SourceName{
			get {
				return this.source_name;
			}
		set {
				this.source_name = value;
			}
		}
		
		public int RawKind {
			get {
				return (int)kind;
			}
			set {
				kind = (PaperSourceKind)value;
			}
		}		  

		internal bool IsDefault {
			get { return is_default;}
			set { is_default = value;}
		}

		public override string ToString(){
			string ret = "[PaperSource {0} Kind={1}]";
			return String.Format(ret, this.SourceName, this.Kind);
		}
		
	}
}
