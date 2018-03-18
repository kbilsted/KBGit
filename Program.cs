using System;
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

		public static void Main() {
			var git = new KBGit("kbilsted", @"c:\temp\kbgit\");
			git.Init();

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
	}
}
