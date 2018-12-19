using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Resx2Xls;

namespace SolutionLocalization
{
	public class ResXToXlsTask : Microsoft.Build.Utilities.Task
	{
		private readonly string[] m_DefaultExcludes = { ".Name", ".ZOrder", ".Parent", ".Type" };

		public string InputPath { get; set; }
		public string OutputXls { get; set; }
		public string[] Culture { get; set; }
		public string[] Excludes { get; set; }
		public string[] DirectoryExclude { get; set; }
		public bool Recursive { get; set; }
		public bool IncludeNonTranslatable { get; set; }

		public ResXToXlsTask()
		{
			Recursive = true;
		}

		public override bool Execute()
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

			if (Excludes == null)
				Excludes = m_DefaultExcludes;

			Resx2XlsForm executor = new Resx2XlsForm();
			executor.ResxToXls(InputPath, Recursive, OutputXls, Culture, Excludes, true, DirectoryExclude, IncludeNonTranslatable);
			return true;
		}
	}
}
