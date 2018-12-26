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
			: base() { }

		[Required]
		public string ServiceUrl { get; set; }
		/// <summary>
		/// Root directory where to create the Resx files
		/// </summary>
		[Required]
		public string OutputDirectory { get; set; }
		/// <summary>
		/// Specified culture for translations; more than one culture can be specified using the
		/// ',' as separator. E.g.: /p:Culture=es,it,pt
		/// </summary>
		[Required]
		public string Culture { get; set; }
		/// <summary>
		/// Xml file with the downloaded translations; if not value is specified, a Data.xml file
		/// is created at the <para>OutputDirectory</para>.
		/// </summary>
		public string DataFile { get; set; }
		/// <summary>
		/// Specifies to download only newer translations; the write date of <para>DataFile</para>
		/// is used for that calc. Default value: true.
		/// </summary>
		public bool Incremental { get; set; } = true;

		public override bool Execute()
		{
			if (String.IsNullOrEmpty(DataFile))
				DataFile = Path.Combine(OutputDirectory, "data.xml");
			if (File.Exists(DataFile) && Incremental)
			{
				DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(DataFile);
				ServiceUrl += "," + lastWriteTimeUtc.ToString("yyyyMMddHHmmss");
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
