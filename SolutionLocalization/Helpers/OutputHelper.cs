using SolutionLocalization.Resources;
using System;

namespace SolutionLocalization.Helpers
{
	static class OutputHelper
	{
		public enum MessageType
		{
			Info,
			Warning,
			Error
		}

		public static void WriteError(string message)
		{
			Write(message, MessageType.Error);
		}

		public static void WriteWarning(string message)
		{
			Write(message, MessageType.Warning);
		}

		public static void Write(string message, MessageType type = MessageType.Info)
		{
			switch(type)
			{
				case MessageType.Error:
					Console.WriteLine(string.Format(Messages.OutputError, message));
					break;
				case MessageType.Warning:
					Console.WriteLine(string.Format(Messages.OutputWarning, message));
					break;
				default:
					Console.WriteLine(message);
					break;
			}
		}
	}
}
