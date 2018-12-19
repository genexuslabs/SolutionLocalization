using System;
using System.Collections.Generic;
using System.IO;

namespace SolutionLocalization.Helpers
{
	static class FileHelper
	{
		static internal bool IsExcluded(string path, string file, string[] excludes)
		{
			string directory = Path.GetDirectoryName(file).Replace(path, "");
		
			if (!directory.StartsWith("\\"))
			{
				directory = "\\" + directory;
			}
			// Exclude the given path; use string.StartsWith() to handle path (not only the directory name),
			// but do not exclude a directory that starts with an exclude name (e.g. do not exclude
			// "DeploymentTarget" directory when we want to exclude "Deploy").
			foreach (string exclude in excludes)
				if (directory.StartsWith(exclude, StringComparison.InvariantCultureIgnoreCase)
					&& (directory.Length == exclude.Length || directory[exclude.Length] == Path.DirectorySeparatorChar))
				{
					return true;
				}
			return false;
		}
	}
}
