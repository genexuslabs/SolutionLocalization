using Microsoft.Build.Framework;
using SolutionLocalization.Tasks;
using System;
using System.IO;
using System.Net;

namespace SolutionLocalization
{
	public class DataToResxTask : Microsoft.Build.Utilities.Task
	{
		public DataToResxTask()
			: base()
		{
			Incremental = true;
		}

		public string DataFile { get; set; }
		[Required]
		public string OutputDirectory { get; set; }
		[Required]
		public string Culture { get; set; }
		[Required]
		public string ServiceUrl { get; set; }
		public bool Incremental { get; set; }

		public override bool Execute()
		{
			//Debug.Assert(false);
			if (String.IsNullOrEmpty(DataFile))
				DataFile = Path.Combine(OutputDirectory, "data.xml");
			if (File.Exists(DataFile) && Incremental)
			{
				DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(DataFile);
				string lastTime = lastWriteTimeUtc.ToString("yyyyMMddHHmmss");
				if (Incremental)
					ServiceUrl += "," + lastTime;
			}
			Directory.CreateDirectory(OutputDirectory);
			using (var client = new WebClient())
			{
				client.DownloadFile(ServiceUrl, DataFile);
			}
			string dataFile = DataFile;
			Directory.CreateDirectory(OutputDirectory);
			DataToResx.ToResx(dataFile, Culture, OutputDirectory);
			return true;
		}
	}
}
