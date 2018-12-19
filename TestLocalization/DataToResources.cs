using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolutionLocalization;
using SolutionLocalization.Tasks;

namespace TestLocalization
{
	[TestClass]
	public class DataToResources
	{
		[TestMethod]
		public void TestGenerateAssemblies()
		{
			GenerateAssembliesCommand task = new GenerateAssembliesCommand();
			task.BasePath = "C:\\testlocalization";
			task.Configuration = "Debug";
			task.Culture = "pt";
			task.FileVersion = "1.1";
			task.KeyFile = @"C:\GeneXus\TeroNet\CommonInfo\Security\Keys\Artech.snk";
			task.ToolsPath = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\";
			task.PlanXml = @"C:\GeneXus\TeroNet\_Tmp\ResourcesCatalog.xml";
			Assert.IsTrue(task.Do());
		}

		[TestMethod]
		public void TestResxGeneration()
		{
			string dataFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "data.xml");
			Directory.CreateDirectory("c:\\testlocalization");
			DataToResx.ToResx(dataFile, "pt", "c:\\testlocalization");
		}

		[TestMethod]
		public void DownloadDataForProjectIDE()
		{
			using (var client = new WebClient())
			{
				client.DownloadFile("http://localhost/GeneXusIDENetSQLServer/atoxml.aspx?1", @"C:\TestLocalizationMSBuild\data.xml");
			}
		}

		[TestMethod]
		public void TestCopyCorresponding()
		{
			string basePath = "C:\\testLocalization";
			string targetPath = basePath + "\\GeneXus\\pt";
			string packagesPath = basePath + "\\GeneXus\\Packages\\pt";
			Directory.CreateDirectory(targetPath);
			Directory.CreateDirectory(packagesPath);

			string gxDir = @"c:\genexus\teronet\deploy\genexus\debug\pt";
			List<string> baseResources = new List<string>(Directory.GetFiles(gxDir, "*.dll"));


			foreach (string resDll in Directory.GetFiles(basePath, "*.dll", SearchOption.AllDirectories))
			{
				string toFind = Path.Combine(gxDir, Path.GetFileName(resDll));
				if (baseResources.Contains(toFind))
					File.Copy(resDll, Path.Combine(targetPath, Path.GetFileName(resDll)), true);
				else
					File.Copy(resDll, Path.Combine(packagesPath, Path.GetFileName(resDll)), true);

			}
		}
	}
}
