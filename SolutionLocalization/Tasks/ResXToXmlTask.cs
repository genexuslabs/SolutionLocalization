using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Resx2Xls;

namespace SolutionLocalization
{
	public class ResXToXmlTask : Microsoft.Build.Utilities.Task
	{
		public string InputPath { get; set; }
		
		public string OutputXls { get; set; }

		public string OutputXml { get; set; }
		public string[] Culture { get; set; }
		public string[] Excludes { get; set; }
		public string[] DirectoryExclude { get; set; }
		public bool Recursive { get; set; }
		public bool IncludeNonTranslatable { get; set; }

		public ResXToXmlTask()
		{
			Recursive = true;
		}

		public override bool Execute()
		{
			if (String.IsNullOrEmpty(OutputXml))
				OutputXml = OutputXls;
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
			new Resx2XlsForm().ResxToXml(InputPath, Recursive, OutputXml, Culture, Excludes, true, DirectoryExclude, IncludeNonTranslatable);
			return true;
		}
	}
}
