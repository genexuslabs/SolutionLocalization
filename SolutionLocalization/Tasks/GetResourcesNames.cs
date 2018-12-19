using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SolutionLocalization
{
	public class GetResourcesNames : Task
	{
		[Required]
		public string[] DefaultOutput { get; set; }
		[Required]
		public string[] FileName { get; set; }

		[Output]
		public string[] FullClassName { get; set; }
		[Required]
		public string BasePath { get; set; }
		[Required]
		public string Culture { get; set; }

		public override bool Execute()
		{
			List<string> outputs = new List<string>();
			for (int i = 0; i < this.FileName.Length; i++)
			{
				string fileName = this.FileName[i];
				fileName = fileName.Replace("." + this.Culture + ".resx", ".cs");
				if (!File.Exists(fileName))
				{
					outputs.Add(Path.Combine(this.BasePath, this.DefaultOutput[i]));
				}
				else
				{
					String ns = "";
					string[] lines = System.IO.File.ReadAllLines(fileName);
					foreach (string line in lines)
					{
						if (line.Trim().StartsWith("//"))
							continue;
						
						if (line.Trim().StartsWith("namespace") && ns.Length == 0)
							ns = line.Replace("namespace", "").Trim();
						if (line.Contains("class"))
						{
							string[] infoClass = line.Replace("abstract", "").Replace("public", "").Replace("private", "").Replace("partial", "").Replace("internal", "").Replace("class", "").Trim().Split(new char[] { ':' });
							ns += "." + infoClass[0].Trim();
							outputs.Add(Path.Combine(this.BasePath, ns + "." + this.Culture + ".resources"));
							break;
						}
					}
				}
			}
			this.FullClassName = outputs.ToArray();
			return true;
		}
	}
}
