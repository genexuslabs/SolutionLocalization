using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace SolutionLocalization
{
	public delegate bool ProcessLine(string text);

	public class ProcessRunnerData
	{
		public ProcessRunnerData(bool MulDrv)
		{
			IsMultipleDriver = MulDrv;
		}
		public bool IsMultipleDriver = false;
		public bool m_ValidOutput = false;
		public string m_FirstSectionName;
		public string m_CurrentSection;
		public void Clear()
		{
			m_ValidOutput = false;
			m_FirstSectionName = null;
			m_CurrentSection = null;
		}
	}

	public class ProcessRunner
	{
		private static bool m_DisplaySectionMsg = true;
		public static bool DisplaySectionMsg
		{
			get { return m_DisplaySectionMsg; }
			set { m_DisplaySectionMsg = value; }
		}
		public static AutoResetEvent EndSectionEvent;
		private StringBuilder m_AllLines;
		private bool m_ShowExternalOutput = false;

		private string m_WorkingDir = null;
		public string WorkingDirectory
		{
			get { return m_WorkingDir; }
			set { m_WorkingDir = value; }
		}

		private bool m_UnescapeEnabled = true;
		public bool UnescapeEnabled
		{
			get { return m_UnescapeEnabled; }
			set { m_UnescapeEnabled = value; }
		}

		private Process m_CurrentProcess;
		public Process Process
		{
			get { return m_CurrentProcess; }
		}

		public ProcessRunner() : this(new ProcessRunnerData(false)) { }

		public ProcessRunner(ProcessRunnerData m_Data)
		{
			if (m_Data == null) Data = new ProcessRunnerData(false);
			else Data = m_Data;
		}

		private ProcessRunnerData m_ProcessRunnerData;
		public ProcessRunnerData Data
		{
			get { return m_ProcessRunnerData; }
			set { m_ProcessRunnerData = value; }
		}

		public bool ShowExternalOutput
		{
			get { return m_ShowExternalOutput; }
			set { m_ShowExternalOutput = value; }
		}

	


		private string startup_fileName, startup_arguments;
		public bool Run(string fileName, string arguments)
		{
			startup_fileName = fileName;
			startup_arguments = arguments;
			m_AllLines = new StringBuilder();
			m_AllLines.AppendLine(fileName + " " + arguments);

			return m_Run();
		}

		private bool m_Run()
		{
			bool ok = false;
			//m_ValidOutput = false;
			m_CurrentProcess = new Process();
			m_CurrentProcess.StartInfo.FileName = startup_fileName;
			m_CurrentProcess.StartInfo.Arguments = startup_arguments;
			m_CurrentProcess.StartInfo.CreateNoWindow = true;
			m_CurrentProcess.StartInfo.UseShellExecute = false;
			m_CurrentProcess.StartInfo.RedirectStandardOutput = true;
			m_CurrentProcess.StartInfo.RedirectStandardError = true;
			m_CurrentProcess.StartInfo.RedirectStandardInput = true;
			if (m_WorkingDir != null)
				m_CurrentProcess.StartInfo.WorkingDirectory = m_WorkingDir;
			m_CurrentProcess.OutputDataReceived += proc_DataReceived;
			m_CurrentProcess.ErrorDataReceived += proc_DataReceived;
			if (m_CurrentProcess.Start())
			{
				m_CurrentProcess.BeginOutputReadLine();
				m_CurrentProcess.BeginErrorReadLine();
				ok = true;
			}
			else
			{
				m_CurrentProcess = null;
				Debug.Assert(false, "Process already started");
			}
			return ok;
		}

		public enum ProcessState
		{
			Initializing,
			Running,
			CancelRequest,
			Canceled,
			Idle
		}

		public void SetState(ProcessState state)
		{
			lock (this)
			{
				m_State = state;
			}
		}

		private ProcessState m_State = ProcessState.Initializing;

		// Kept for backward campatibility
		public bool Wait()
		{
			int exitCode;
			return (Wait(0, out exitCode));
		}

		// Kept for backward campatibility
		public bool Wait(int noerrorExitCode)
		{
			int exitCode;
			return (Wait(noerrorExitCode, out exitCode));
		}

		public bool Wait(out int exitCode)
		{
			return (Wait(0, out exitCode));
		}

		public bool Wait(int noerrorExitCode, out int exitCode)
		{
			bool ok;
			try
			{
				lock (this)
				{
					bool needCancel = m_State == ProcessState.CancelRequest;
					m_State = ProcessState.Running;
					if (needCancel)
						Cancel();
				}
				m_CurrentProcess.WaitForExit();
				exitCode = m_CurrentProcess.ExitCode;
				ok = (m_CurrentProcess.ExitCode == noerrorExitCode);
				m_CurrentProcess.Close();
				m_CurrentProcess.Dispose();
		
			}
			finally
			{
				m_State = ProcessState.Initializing;
				m_CurrentProcess = null;
				m_AllLines = null;
			}
			return ok;
		}

		private static string UnescapeReturn(string value)
		{
			IDictionary<char, char> map = new Dictionary<char, char>();
			map.Add('n', '\n');
			map.Add('r', '\r');
			map.Add('\\', '\\');
			return Unescape(value, '\\', map);
		}

		private static string UnescapeSeparator(string value)
		{
			IDictionary<char, char> map = new Dictionary<char, char>();
			map.Add('s', '|');
			map.Add('-', '-');
			return Unescape(value, '-', map);
		}

		private static string Unescape(string value, char escapeChar, IDictionary<char, char> map)
		{
			StringBuilder newValue = new StringBuilder();
			int start = 0;
			while (start < value.Length)
			{
				int escapePos = value.IndexOf(escapeChar, start);
				if (escapePos == -1 || escapePos + 1 >= value.Length)
				{
					newValue.Append(value.Substring(start));
					break;
				}

				newValue.Append(value.Substring(start, escapePos - start));
				if (map.Keys.Contains(value[escapePos + 1]))
				{
					newValue.Append(map[value[escapePos + 1]]);
					start = escapePos + 2;
				}
				else
					start = escapePos + 1;
			}

			return newValue.ToString();
		}

		public bool SectionOpen
		{
			get { return Data.m_ValidOutput; }
		}

		private void proc_DataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				Console.WriteLine(e.Data);
			}
	
		}

		public void Cancel()
		{
		
		}

	
	}
}
