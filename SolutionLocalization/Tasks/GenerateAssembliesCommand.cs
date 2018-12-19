using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SolutionLocalization.Tasks
{
	public class GenerateAssembliesCommand
	{
		public GenerateAssembliesCommand()
		{
			Configuration = "Release";
			IntermediateDirectory = "SatelliteResources";
		}

		public string PlanXml { get; set; }
		public string BasePath { get; set; }
		public string Culture { get; set; }
		public string Configuration { get; set; }
		public string FileVersion { get; set; }
		public string KeyFile { get; set; }
		public string SpecificAssembly { get; set; }
		public string ToolsPath { get; set; }
		public string IntermediateDirectory { get; set; }
		private string FullOutputDirectory { get; set; }

		public bool Do()
		{
			if (String.IsNullOrEmpty(FileVersion))
				FileVersion = "10.1.0.0";
			//Debug.Assert(false);
			if (String.IsNullOrEmpty(PlanXml) || !File.Exists(PlanXml))
			{
				Console.WriteLine(string.Format("error: invalid file '{0}', we need a plan to build satellite assemblies", PlanXml));
				return false;
			}
			if (String.IsNullOrEmpty(ToolsPath))
			{
				ToolsPath = Environment.GetEnvironmentVariable("WindowsSDK_ExecutablePath_x64");
				if (String.IsNullOrEmpty(ToolsPath))
				{
					ToolsPath = Environment.GetEnvironmentVariable("WindowsSDK_ExecutablePath_x86");
					if (String.IsNullOrEmpty(ToolsPath))
					{
						Console.WriteLine("error: invalid ToolsPath");
						return false;
					}
				}
			}
			FullOutputDirectory = Path.IsPathRooted(IntermediateDirectory) ? IntermediateDirectory : Path.Combine(BasePath, IntermediateDirectory);

			try
			{
				List<SatelliteAssembly> assemblies = GetAssemblies(PlanXml, Culture, BasePath, Configuration, FullOutputDirectory);
				if (!String.IsNullOrEmpty(KeyFile))
					assemblies.ForEach(ass => ass.KeyFile = KeyFile);
				Console.WriteLine("Generate Satellite Assemblies");
				GenerateSatelliteAssemblies(assemblies);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("error: generation of satellite assemblies failed (exception: {0})", ex.Message));
				return false;
			}

		}

		public static List<SatelliteAssembly> GetAssemblies(string planXml, string culture, string basePath, string configuration, string fullOutputDirectory)
		{
			List<SatelliteAssembly> assemblies = new List<SatelliteAssembly>();
			XmlDocument doc = new XmlDocument();
			doc.Load(planXml);

			Console.WriteLine("Reading Satellite Assembly Generation Plan");

			foreach (XmlNode sAssembly in doc.SelectNodes("/Resources/Resource"))
			{
				SatelliteAssembly assembly = new SatelliteAssembly()
				{
					Name = sAssembly.SelectSingleNode("AssemblyName").InnerText,
					Location = sAssembly.SelectSingleNode("Location").InnerText,
				};
				if (assembly.Name.Length > 0)
				{
					try
					{
						string projectOutDir = GetProjectOutputDir(assembly, basePath, configuration);
						assembly.FullOutputPath = Path.Combine(Path.Combine(projectOutDir, culture), sAssembly.SelectSingleNode("SatelliteAssembly").InnerText);
						assembly.Culture = culture;
						assembly.Files = new List<ResXFile>();
						XmlNode node = sAssembly.SelectSingleNode("AssemblyType");
						try
						{
							if (node != null)
								assembly.Type = (SatelliteAssembly.AssemblyType)Enum.Parse(typeof(SatelliteAssembly.AssemblyType), node.InnerText);
						}
						catch { }
						node = sAssembly.SelectSingleNode("AssemblyKeyFile");
						if (node != null)
						{
							if (Path.IsPathRooted(node.InnerText))
								assembly.KeyFile = node.InnerText;
							else
								assembly.KeyFile = Path.Combine(Path.Combine(basePath, assembly.Location), node.InnerText);
						}
						foreach (XmlNode resx in sAssembly.SelectNodes("Resx"))
						{
							string logicalName = string.Format("{0}.{1}.resources", resx.SelectSingleNode("LogicalName").InnerText, culture);
							ResXFile file = new ResXFile()
							{
								Name = resx.SelectSingleNode("Name").InnerText.Replace(assembly.Location, ""),
								LogicalName = logicalName,
								Output = Path.Combine(fullOutputDirectory, logicalName),
								RelativePath = resx.SelectSingleNode("RelativePath").InnerText
							};
							assembly.Files.Add(file);
						}
						if (assembly.Name.Length > 0)
						{
							assemblies.Add(assembly);
						}
					}
					catch
					{
						Console.WriteLine(string.Format("Error for '{0}'", assembly.Name));
					}
				}
			}
			return assemblies;

		}

		private void GenerateSatelliteAssemblies(List<SatelliteAssembly> assemblies)
		{
			foreach (SatelliteAssembly ass in assemblies)
			{
				Console.WriteLine(string.Format("=== Start generation of '{0}' assembly ===", ass.Name));
				if (String.IsNullOrEmpty(SpecificAssembly))
				{
					GenerateSatelliteAssembly(ass, FullOutputDirectory, Culture, BasePath, ToolsPath, FileVersion, Configuration);
				}
				else if (ass.Name == SpecificAssembly)
				{
					GenerateSatelliteAssembly(ass, FullOutputDirectory, Culture, BasePath, ToolsPath, FileVersion, Configuration);
					return;
				}
			}
		}

		public static void GenerateSatelliteAssembly(SatelliteAssembly ass, string fullOutputDirectory, string culture, string basePath, string toolsPath, string fileVersion, string configuration)
		{
			if (GenerateResources(ass, fullOutputDirectory, culture, basePath, toolsPath))
				LinkResources(ass, fileVersion, ass.KeyFile, culture, toolsPath, basePath, configuration);
		}

		private static bool LinkResources(SatelliteAssembly ass, string fileVersion, string keyFile, string culture, string toolsPath, string basePath, string configuration)
		{
			int exitCode = -1;
			StringBuilder linkArguments = new StringBuilder();
			foreach (ResXFile resx in ass.Files.Where(x => !string.IsNullOrEmpty(x.CalculatedOutput) && File.Exists(x.CalculatedOutput)))
				linkArguments.AppendFormat(" /embed:\"{0}\"", resx.CalculatedOutput);
			if (linkArguments.Length > 0)
			{
				string outputFile = ass.FullOutputPath;
				Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

				string templateAssemblyFile = Path.Combine(GetProjectOutputDir(ass, basePath, configuration), string.Format("{0}.{1}", ass.Name, ass.Type));
				string assemblyKeyFile = !string.IsNullOrEmpty(ass.KeyFile) ? ass.KeyFile : keyFile;
				try
				{
					StringBuilder allArguments = new StringBuilder();
					allArguments.AppendFormat("/culture:{0}", culture);
					allArguments.AppendFormat(" /out:{0}", outputFile);
					if (!string.IsNullOrEmpty(assemblyKeyFile) && !File.Exists(assemblyKeyFile))
					{
						throw new Exception(string.Format("error: cannot define a strong name; 1. invalid template file '{0}' and 2. invalid strong name file '{1}' -> cannot generate '{2}'", templateAssemblyFile, keyFile, outputFile));
					}
					if (!String.IsNullOrEmpty(assemblyKeyFile))
						allArguments.AppendFormat(" /KeyFile:{0}", assemblyKeyFile);
					if (File.Exists(templateAssemblyFile))
						allArguments.AppendFormat(" /template:{0}", templateAssemblyFile);
					else
						allArguments.AppendFormat(" /version:{0} /fileversion:{0}", fileVersion);
					allArguments.Append(linkArguments.ToString());

					string executable = "al.exe";
					if (!string.IsNullOrEmpty(toolsPath))
						executable = Path.Combine(toolsPath, executable);

					Console.WriteLine(string.Format("[Info] Executing: {0} {1}", executable, allArguments.ToString()));

					ProcessRunner runner = new ProcessRunner();
					runner.Run(executable, allArguments.ToString());
					runner.Wait(out exitCode);
					if (exitCode == 0)
						ass.FullOutputPath = outputFile;
					else
						Console.WriteLine(string.Format("error: Cannot generate satellite assembly (exit code: {0})", exitCode));
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
			Console.WriteLine("=> " + (exitCode == 0 ? "Successful" : "Failed"));
			return exitCode == 0; // Success
		}

		private static bool GenerateResources(SatelliteAssembly ass, string fullOutputDirectory, string culture, string basePath, string toolsPath)
		{
			StringBuilder compileArguments = new StringBuilder();
			Directory.CreateDirectory(fullOutputDirectory);
			foreach (ResXFile resx in ass.Files)
			{
				string resxFile = String.IsNullOrEmpty(resx.File) ? Path.Combine(basePath, Path.Combine(Path.Combine(ass.Location, resx.RelativePath), resx.Name).Replace(".resx", "." + culture + ".resx")) : resx.File;
				string resxOutput = Path.Combine(basePath, resx.Output);
				resx.CalculatedOutput = resxOutput;
				if (File.Exists(resxFile))
					compileArguments.Append(String.Format(" \"{0},{1}\"", resxFile, resxOutput));
			}
			int exitCode = -1;
			if (compileArguments.ToString().Length > 0)
			{
				Size sz = new Size();
				string refS = "/r:" + sz.GetType().Assembly.Location;
				StringBuilder allArguments = new StringBuilder(refS + " /useSourcePath /publicClass /compile");
				allArguments.Append(compileArguments.ToString());

				string executable = "resgen.exe";
				if (!string.IsNullOrEmpty(toolsPath))
					executable = Path.Combine(toolsPath, executable);

				Console.WriteLine(string.Format("[Info] Executing: {0} {1}", executable, allArguments.ToString()));

				ProcessRunner runner = new ProcessRunner();
				runner.Run(executable, allArguments.ToString());
				runner.Wait(out exitCode);

				Console.WriteLine("[Info] ResGen.exe execution " + (exitCode == 0 ? "was success" : "has failed"));
			}
			else
			{
				Console.WriteLine("[Info] No resources found.");
			}
			return exitCode == 0; // Sucess
		}

		private static string GetProjectOutputDir(SatelliteAssembly assembly, string basePath, string configuration)
		{
			return Path.Combine(Path.Combine(Path.Combine(basePath, assembly.Location), "bin"), configuration);
		}
	}
}
