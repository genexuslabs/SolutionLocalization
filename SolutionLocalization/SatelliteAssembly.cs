using System;
using System.Collections.Generic;
using System.Text;

namespace SolutionLocalization
{
	public class SatelliteAssembly
	{
		public enum AssemblyType
		{
			dll,
			exe
		}
		public SatelliteAssembly()
		{
			Type = AssemblyType.dll;
		}
		public string Name { get; set; }
		public string Location { get; set; }
		public string Culture { get; set; }
		public List<ResXFile> Files { get; set; }
		public string FullName
		{
			get { return String.Format(@"{0}\{1}.resources.dll", Culture, Name); }
		}
		public string FullOutputPath { get; set; }
		public string RootNamespace { get; set; }
		public AssemblyType Type { get; set; }
		public string KeyFile { get; set; }
	}

	public class ResXFile
	{
		public string Name { get; set; }
		public string LogicalName { get; set; }
		public string Output { get; set; }
		public string CalculatedOutput { get; set; }
		public string RelativePath { get; set; }
		public string File { get; set; }
	}
}
