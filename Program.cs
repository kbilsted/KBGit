using System;
using System.IO;

namespace KbgSoft.KBGit
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var output = new CommandLineHandling().Handle(new KBGit(new DirectoryInfo(".").FullName), CommandLineHandling.Config, args);
			Console.WriteLine(output);
		}
	}
}
