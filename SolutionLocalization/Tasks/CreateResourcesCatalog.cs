
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using SolutionLocalization.Utils;

namespace SolutionLocalization
{
	public class CreateResourcesCatalog : Task
	{
		[Required]
		public string BasePath { get; set; }
		[Required]
		public string SerializedPath { get; set; }
		public string[] DirectoryExclude { get; set; }

		public void GetCatalog()
		{
			List<SatelliteAssembly> assemblies = new List<SatelliteAssembly>();

			var xmlNSMgr = new XmlNamespaceManager(new NameTable());
			xmlNSMgr.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");

			DirectoryInfo di = new DirectoryInfo(BasePath);
			foreach (FileInfo info in di.GetFilesByPattern("*.csproj"))
			{
				if (Helpers.FileHelper.IsExcluded(BasePath, info.FullName, DirectoryExclude))
					continue;
				SatelliteAssembly assembly;
				XmlDocument doc = null;
				try
				{
					doc = new XmlDocument();
					doc.Load(info.FullName);

					assembly = new SatelliteAssembly()
					{
						Name = doc.SelectSingleNode("msb:Project/msb:PropertyGroup/msb:AssemblyName", xmlNSMgr).InnerText.Trim(),
						RootNamespace = doc.SelectSingleNode("msb:Project/msb:PropertyGroup/msb:RootNamespace", xmlNSMgr).InnerText.Trim(),
						Location = Path.GetDirectoryName(info.FullName).Replace(BasePath + Path.DirectorySeparatorChar, string.Empty),
						Files = new List<ResXFile>()
					};
				}
				catch (Exception ex)
				{
					Console.WriteLine(string.Format("error: Cannot read '{0}' file (exception: {1})", info.FullName, ex.Message));
					continue;
				}
				try
				{
					string assemblyType = doc.SelectSingleNode("msb:Project/msb:PropertyGroup/msb:OutputType", xmlNSMgr).InnerText.Trim();
					if (assemblyType == "WinExe")
						assembly.Type = SatelliteAssembly.AssemblyType.exe;
				}
				catch { }
				try
				{
					assembly.KeyFile = doc.SelectSingleNode("msb:Project/msb:PropertyGroup/msb:AssemblyOriginatorKeyFile", xmlNSMgr).InnerText.Trim();
				}
				catch { }

				Console.WriteLine(string.Format("[Info] Reading '{0}' file", info.FullName));

				try
				{
					foreach (XmlNode embResEntry in doc.SelectNodes("msb:Project/*/msb:EmbeddedResource/@Include", xmlNSMgr))
					{
						string embResourceFile = embResEntry.InnerText;
						int lastIdx = embResourceFile.LastIndexOf(Path.DirectorySeparatorChar);
						string embResourceFilename = lastIdx > 0 ? embResourceFile.Substring(lastIdx + 1) : embResourceFile;
						if (embResourceFilename.Replace(".resx", "").Contains(".")) // only default resources computed
							continue;
						ResXFile resxFile = new ResXFile()
						{
							Name = embResourceFilename,
							RelativePath = (lastIdx > 0 ? embResourceFile.Substring(0, lastIdx) : string.Empty)
						};
						resxFile.LogicalName = GetLogicalName(BasePath, assembly, resxFile);
						assembly.Files.Add(resxFile);
					}

					if (assembly.Files.Count > 0)
					{
						Console.WriteLine(string.Format("  -> Has '{0}' resources included", assembly.Files.Count));
						assemblies.Add(assembly);
					}
					else
					{
						Console.WriteLine("  .> Ignored");
					}
				}
				catch
				{ }
			}

			Console.WriteLine(string.Format("[Info] Writing '{0}' catalog...", SerializedPath));

			using (StreamWriter writer = File.CreateText(SerializedPath))
			{
				using (XmlTextWriter xmlWriter = new XmlTextWriter(writer))
				{
					xmlWriter.WriteStartDocument();
					xmlWriter.WriteStartElement("Resources");
					xmlWriter.WriteElementString("BasePath", BasePath);

					foreach (SatelliteAssembly it in assemblies)
					{
						xmlWriter.WriteStartElement("Resource");
						xmlWriter.WriteElementString("AssemblyName", it.Name);
						xmlWriter.WriteElementString("SatelliteAssembly", it.Name + ".resources.dll");
						xmlWriter.WriteElementString("Location", it.Location);
						if (it.Type != SatelliteAssembly.AssemblyType.dll)
							xmlWriter.WriteElementString("AssemblyType", Enum.GetName(typeof(SatelliteAssembly.AssemblyType), it.Type));
						if (!string.IsNullOrEmpty(it.KeyFile))
							xmlWriter.WriteElementString("AssemblyKeyFile", it.KeyFile);
						foreach (ResXFile resx in it.Files)
						{
							xmlWriter.WriteStartElement("Resx");
							xmlWriter.WriteElementString("Name", resx.Name);
							xmlWriter.WriteElementString("RelativePath", resx.RelativePath);
							xmlWriter.WriteElementString("LogicalName", resx.LogicalName);
							if (!string.IsNullOrEmpty(resx.CalculatedOutput))
								xmlWriter.WriteElementString("Resource", resx.CalculatedOutput);
							xmlWriter.WriteEndElement();
						}

						xmlWriter.WriteEndElement();
					}
					xmlWriter.WriteEndElement();
					xmlWriter.WriteEndDocument();
				}
			}
		}

		static Regex NS_DEF_REGEX = new Regex(@"^\s*namespace\s+(?<NS>[^\s{/]+)");
		static Regex CLASS_DEF_REGEX = new Regex(@"((public|private|internal)\s+)?((partial|abstract|static)\s+)?class\s+(?<ClassName>[^\s:<{/]+)");
		private string GetLogicalName(string basePath, SatelliteAssembly assembly, ResXFile resxFile)
		{
			string ns = string.Empty;
			string fullFileName = Path.GetFullPath(Path.Combine(Path.Combine(basePath, assembly.Location), string.IsNullOrEmpty(resxFile.RelativePath) ? resxFile.Name : Path.Combine(resxFile.RelativePath, resxFile.Name)));
			string fileName = fullFileName.Replace(".resx", ".Designer.cs");
			if (!File.Exists(fileName))
				fileName = fullFileName.Replace(".resx", ".cs");
			if (File.Exists(fileName))
			{
				int step = 0;
				string[] lines = System.IO.File.ReadAllLines(fileName);
				foreach (string line in lines)
				{
					if (line.Trim().StartsWith("//"))
						continue;
					switch (step)
					{
						case 0:
							if (NS_DEF_REGEX.IsMatch(line))
							{
								ns = NS_DEF_REGEX.Match(line).Groups["NS"].ToString();
								step++;
							}
							break;
						case 1:
							if (CLASS_DEF_REGEX.IsMatch(line))
							{
								ns += "." + CLASS_DEF_REGEX.Match(line).Groups["ClassName"].ToString();
								step++;
							}
							break;
					}
					if (step > 1)
						break;
				}
			}
			if (string.IsNullOrEmpty(ns))
				ns = string.Format("{0}{1}.{2}", assembly.RootNamespace, resxFile.RelativePath.Length > 0 ? "." + resxFile.RelativePath.Replace(Path.DirectorySeparatorChar, '.') : string.Empty, Path.GetFileNameWithoutExtension(resxFile.Name));
			return ns;
		}

		public override bool Execute()
		{
			GetCatalog();
			return true;
		}
	}
}
