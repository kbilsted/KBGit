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

			var path = @"c:\temp\kbgit\129837921734298\";
			if (Directory.Exists(path))
				Directory.Delete(path, true);
			Directory.CreateDirectory(path);
			
			//Call(@"c:\temp\kbgit\129837921734298\");
			var git = new KBGit("");
			git.Init();
			//git.CommandLineHandling("init");
			File.WriteAllText(git.CodeFolder + "file.txt", "Hello world");
			git.Commit("Adding note", "kasper", DateTime.Now, git.ScanFileSystem());
			Console.WriteLine(git.Log());
			WaitKey();

			Console.WriteLine("---");
			File.WriteAllText(git.CodeFolder + "file.txt", "Hello world\nAnd more stuff here");
			File.WriteAllText(git.CodeFolder + "readme.md", "#title\n\nthis module is bla bla\nand then more bla bla");
			git.Commit("Adding extra text and readme", "kasper", DateTime.Now, git.ScanFileSystem());
			Console.WriteLine(git.Log());

			Console.WriteLine("---");
			File.WriteAllText(git.CodeFolder + "file.txt",
				"# title\n\nthis module is bla bla\nand then more bla bla" + "\nso much\nmoooore\n123....wow\nlalala");
			git.Commit("More text to file1", "kasper", DateTime.Now, git.ScanFileSystem());
			Console.WriteLine(git.Log());
			WaitKey();

			Console.WriteLine("---");
			Console.WriteLine("creating branch");
			git.CheckOut_b("Feature1", git.HeadRef(1));
			File.WriteAllText(git.CodeFolder + "featurefile.cs",
				"class Feature \n{ Some cool feature \n}" + "\nso much\nmoooore\n123....wow\nlalala");
			git.Commit("Add feature 1", "kasper", DateTime.Now, git.ScanFileSystem());
			Console.WriteLine(git.Log());
			WaitKey();

			Console.WriteLine("reset to main");
			git.Checkout("master");
			WaitKey();
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
