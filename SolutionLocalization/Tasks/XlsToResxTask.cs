using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Resx2Xls;

namespace SolutionLocalization
{
	public class XlsToResxTask : Task
    {
		[Required]
		public string InputXls { get; set; }
		public string XmlValidEntriesFile { get; set; }
		public string OutputPath { get; set; }

		public override bool Execute()
		{
			if (string.IsNullOrEmpty(OutputPath))
				OutputPath = ".";
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
			(new Resx2XlsForm()).XlsToResx(InputXls, XmlValidEntriesFile, OutputPath);
			return true;
		}
	}
}
