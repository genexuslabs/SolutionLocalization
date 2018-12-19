using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SolutionLocalization.Tasks;
using SolutionLocalization.Utils;
using System;
using System.IO;

namespace SolutionLocalization
{
	public class GenerateAssemblies : Task
	{
		public GenerateAssemblies()
			: base()
		{
			Configuration = "Release";
			IntermediateDirectory = "SatelliteResources";
		}

		[Required]
		public string PlanXml { get; set; }
		[Required]
		public string BasePath { get; set; }
		[Required]
		public string Culture { get; set; }
		public string Configuration { get; set; }
		public string FileVersion { get; set; }
		public string KeyFile { get; set; }
		public string SpecificAssembly { get; set; }
		public string ToolsPath { get; set; }
		public string IntermediateDirectory { get; set; }
		public override bool Execute()
		{
			if (String.IsNullOrEmpty(KeyFile) || !File.Exists(KeyFile))
			{
				Console.WriteLine("Empty or Invalid KeyStore File, taking Artech.snk from the current assembly directory");
				KeyFile = Path.Combine(Assemblies.GetAssemblyDirectory(), "Artech.snk");
			}

			GenerateAssembliesCommand cmd = new GenerateAssembliesCommand()
			{
				PlanXml = PlanXml,
				BasePath = BasePath,
				Culture = Culture,
				Configuration = Configuration,
				FileVersion = FileVersion,
				KeyFile = KeyFile,
				SpecificAssembly = SpecificAssembly,
				ToolsPath = ToolsPath,
				IntermediateDirectory = IntermediateDirectory
			};
			return cmd.Do();
		}


	}
}
