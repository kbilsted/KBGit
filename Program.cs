using System;
using System.Diagnostics;
using System.IO;

namespace KbgSoft.KBGit {
	public class Program {
		static void WaitKey() {
			var f = Console.ForegroundColor;
			do {
				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.WriteLine("Press enter...");
			} while (Console.ReadKey().Key != ConsoleKey.Enter);

			Console.ForegroundColor = f;
		}

		public static void Main(string[] args)
		{
			Console.WriteLine("**: "+args.Length);
			Console.WriteLine(string.Join("\n***: ", args));

			Console.WriteLine(new CommandLineHandling().Handle(new KBGit(new DirectoryInfo(".").FullName), CommandLineHandling.Config, args));
		}

		public static void Call(string workingDirectory)
		{
			using (System.Diagnostics.Process pProcess = new System.Diagnostics.Process())
			{
				pProcess.StartInfo.WorkingDirectory = workingDirectory;
				pProcess.StartInfo.FileName = @"C:\src\KBGit\obj\netcoreapp2.0\win-x64\host\KBGit.exe";
				pProcess.StartInfo.Arguments = @"init"; 
				pProcess.StartInfo.UseShellExecute = true;
				pProcess.StartInfo.RedirectStandardOutput = false;
				pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
				pProcess.StartInfo.CreateNoWindow = false; //not diplay a windows
				pProcess.Start();
				pProcess.WaitForExit();
			}
		}

	}
}
