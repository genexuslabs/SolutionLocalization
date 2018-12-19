using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SolutionLocalization
{
	public class SaveInfo : Task
	{
		private static List<ITaskItem> sInfos = new List<ITaskItem>();
		public string SatelliteAssembly { get; set; }
		public string AssemblyName { get; set; }
		public string Location { get; set; }
		public string Culture { get; set; }
		public string SerializePath { get; set; }
		public string BaseDir { get; set; }

		public ITaskItem[] ResxFiles { get; set; }
		public ITaskItem[] ResourcesFiles { get; set; }

		[Output]
		public ITaskItem[] Satellites { get; set; }


		public override bool Execute()
		{
			if (!String.IsNullOrEmpty(SerializePath))
			{
				using (StreamWriter writer = File.CreateText(SerializePath))
				{
					XmlTextWriter xmlWriter = new XmlTextWriter(writer);
					xmlWriter.WriteStartDocument();
					xmlWriter.WriteStartElement("Resources");
					xmlWriter.WriteElementString("BasePath", BaseDir);
					
					foreach (ResXItem it in sInfos)
					{
						string basePath = it.GetMetadata("BaseDir");
						xmlWriter.WriteStartElement("Resource");
						xmlWriter.WriteElementString("AssemblyName", it.GetMetadata("AssemblyName"));
						xmlWriter.WriteElementString("SatelliteAssembly", it.GetMetadata("SatelliteAssembly"));
						xmlWriter.WriteElementString("Location", it.GetMetadata("Location").Replace(basePath, ""));
						xmlWriter.WriteElementString("Culture", it.GetMetadata("Culture"));
						ITaskItem[] resxFiles = it.ResXFiles;
						ITaskItem[] resources = it.Resources;
						for (int i = 0; i < resxFiles.Length; i++)
						{
							xmlWriter.WriteStartElement("Resx");
							xmlWriter.WriteElementString("Name", resxFiles[i].ToString().Replace(basePath, ""));
							xmlWriter.WriteElementString("LogicalName", resxFiles[i].GetMetadata("OutputResource").Replace(basePath, ""));
							xmlWriter.WriteElementString("Resource", resources[i].ToString().Replace(basePath, ""));
							xmlWriter.WriteEndElement();
						}

						xmlWriter.WriteEndElement();
					}
					xmlWriter.WriteEndElement();
					xmlWriter.WriteEndDocument();
				}
				return true;
			}

			if (String.IsNullOrEmpty(SatelliteAssembly))
			{
				Satellites = sInfos.ToArray();
				return true;
			}

			ResXItem item = new ResXItem(SatelliteAssembly);

			item.SetMetadata("SatelliteAssembly", SatelliteAssembly);
			item.SetMetadata("AssemblyName", AssemblyName);
			item.SetMetadata("Location", Location);
			item.SetMetadata("Culture", Culture);
			item.SetMetadata("BaseDir", BaseDir);
			
			item.ResXFiles = ResxFiles;
			item.Resources = ResourcesFiles;
			sInfos.Add(item);
			Satellites = sInfos.ToArray();

		

			return true;
		}
	}

	public class ResXItem : ITaskItem
	{
		public ResXItem(string itm)  
		{
			item = new TaskItem(itm);
		}
		public TaskItem item = new TaskItem();
		public ITaskItem[] ResXFiles { get; set; }
		public ITaskItem[] Resources { get; set; }


		#region ITaskItem Members

		public System.Collections.IDictionary CloneCustomMetadata()
		{
			return item.CloneCustomMetadata();
		}

		public void CopyMetadataTo(ITaskItem destinationItem)
		{
			item.CopyMetadataTo(destinationItem);
		}

		public string GetMetadata(string metadataName)
		{
			return item.GetMetadata(metadataName);
		}

		public string ItemSpec
		{
			get
			{
				return item.ItemSpec;
			}
			set
			{
				item.ItemSpec = value;
			}
		}

		public int MetadataCount
		{
			get { return item.MetadataCount; }
		}

		public System.Collections.ICollection MetadataNames
		{
			get { return item.MetadataNames; }
		}

		public void RemoveMetadata(string metadataName)
		{
			item.RemoveMetadata(metadataName);
		}

		public void SetMetadata(string metadataName, string metadataValue)
		{
			item.SetMetadata(metadataName, metadataValue);
		}

		#endregion
	}

	
}
