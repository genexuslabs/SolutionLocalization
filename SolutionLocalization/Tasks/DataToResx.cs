using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Xml;

namespace SolutionLocalization.Tasks
{
	public class DataToResx
	{

		public static void ToResx(string dataFile, string culture, string outDir)
		{
		//	Debug.Assert(false);
			XmlDocument doc = new XmlDocument();
			doc.Load(dataFile);

			Dictionary<string, List<XmlNode>> resources = new Dictionary<string, List<XmlNode>>();
			foreach (XmlNode node in doc.SelectNodes("//Message"))
			{
				string resName = node.SelectSingleNode("ResourceFile").InnerText;
				if (!resources.ContainsKey(resName))
					resources[resName] = new List<XmlNode>();
				resources[resName].Add(node);
			}

	
			foreach (var resource in resources)
			{
				List<XmlNode> messages = resource.Value;
				string resourceFile = resource.Key;

				string targetDirectory = Path.Combine(outDir, Path.GetDirectoryName(JustStemWithFullPath(resourceFile)));
				if (!Directory.Exists(targetDirectory))
				{
					Directory.CreateDirectory(targetDirectory);
				}
				string f = targetDirectory + "\\" + Path.GetFileNameWithoutExtension(resourceFile) + "." + culture + ".resx";

				FileInfo fileInfo = new FileInfo(f);
				using (StreamWriter sw = new StreamWriter(f, false, Encoding.UTF8))
				{
					using (ResXResourceWriter rw = new ResXResourceWriter(sw))
					{
						foreach (var resourceMessage in messages)
						{
							string key = resourceMessage.SelectSingleNode("ResourceKey").InnerText;
							XmlNode nodeValue = resourceMessage.SelectSingleNode("Translation/Text[contains(@Culture, \"" + culture + "\")]");
							if (nodeValue != null)
							{
								string data = nodeValue.InnerText;
								if (!String.IsNullOrEmpty(key))
								{
									ResXDataNode node = new ResXDataNode(key, data)
									{
										Comment = resourceMessage.SelectSingleNode("Comment").InnerText
									};
									rw.AddResource(node);
								}
							}
						}
					}
				}
			}
		}

		private static string JustStemWithFullPath(string cPath)
		{
			//Get the name of the file
			string lcFileName = cPath.Trim();

			//Remove the extension and return the string
			if (lcFileName.IndexOf(".") == -1)
				return lcFileName;
			else
				return lcFileName.Substring(0, lcFileName.LastIndexOf('.'));
		}


	}
}
