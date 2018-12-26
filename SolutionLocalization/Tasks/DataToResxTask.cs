using Microsoft.Build.Framework;
using SolutionLocalization.Tasks;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

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
                Log.LogMessage(String.Format(Resources.Messages.DownloadingDataFrom, ServiceUrl));
                client.DownloadProgressChanged += WebClientDownloadProgressChanged;
                
                Task downloadFile = client.DownloadFileTaskAsync(ServiceUrl, DataFile);
                Task.WaitAll(downloadFile);
			}
			string dataFile = DataFile;
			Directory.CreateDirectory(OutputDirectory);

            string[] cultures = Culture.Split(new char[] { ',' });
            foreach (string culture in cultures)
            {
                Log.LogMessage(String.Format(Resources.Messages.StartCultureResxGeneration, culture));
                DataToResx.ToResx(dataFile, culture, OutputDirectory);
            }
			return true;
		}

        void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Log.LogMessage(String.Format(Resources.Messages.DownloadStatus, e.ProgressPercentage));
        }
    }
}
