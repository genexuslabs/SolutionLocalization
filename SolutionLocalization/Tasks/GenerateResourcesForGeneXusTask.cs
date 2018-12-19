using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionLocalization
{
	public class GenerateResourcesForGeneXusTask : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string BasePath { get; set; }
		[Required]
		public string GeneXusPath { get; set; }
		[Required]
		public string Culture { get; set; }

		public override bool Execute()
		{
	//		Debug.Assert(false);
			string targetPath = Path.Combine(BasePath , "GeneXus\\" + Culture);
			string packagesPath = Path.Combine( BasePath ,  "GeneXus\\Packages\\" + Culture);
			Directory.CreateDirectory(targetPath);
			Directory.CreateDirectory(packagesPath);
			 
			string gxDir = Path.Combine(GeneXusPath, Culture);
			List<string> baseResources = new List<string>(Directory.GetFiles(gxDir, "*.dll"));

			foreach (string resDll in Directory.GetFiles(BasePath, "*.dll", SearchOption.AllDirectories))
			{
				if (Path.GetDirectoryName(resDll) == targetPath || Path.GetDirectoryName(resDll) == packagesPath)
					continue;
				string toFind = Path.Combine(gxDir, Path.GetFileName(resDll));
				if (baseResources.Contains(toFind))
					File.Copy(resDll, Path.Combine(targetPath, Path.GetFileName(resDll)), true);
				else
					File.Copy(resDll, Path.Combine(packagesPath, Path.GetFileName(resDll)), true);

			}
			return true;
		}
	}
}
