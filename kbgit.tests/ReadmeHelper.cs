using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using TeamBinary.LineCounter;
using Xunit;

namespace kbgit.tests
{
	public class ReadMeHelper
	{
		[Fact]
		public void MutateReadme()
		{
			var basePath = Path.Combine(Assembly.GetExecutingAssembly().Location, "..", "..", "..", "..", "..");

			var stats = new DirWalker().DoWork(new[] {Path.Combine(basePath, "Git.cs")});

			var shieldsRegEx = new Regex("<!--start-->.*<!--end-->", RegexOptions.Singleline);
			var githubShields = new WebFormatter().CreateGithubShields(stats);

			var readmePath = Path.Combine(basePath, "README.md");
			var oldReadme = File.ReadAllText(readmePath);
			var newReadMe = shieldsRegEx.Replace(oldReadme, $"<!--start-->\r\n{githubShields}\r\n<!--end-->");

			if (oldReadme != newReadMe)
				File.WriteAllText(readmePath, newReadMe);
		}
	}
}
