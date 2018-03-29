using System;
using System.IO;
using KbgSoft.KBGit;

namespace kbgit.tests
{
	public class RepoBuilder
	{
		readonly string basePath;
		public KBGit Git;

		public RepoBuilder() : this(@"c:\temp\", Guid.NewGuid()) { }
		public RepoBuilder(Guid unittestguid) : this(@"c:\temp\", unittestguid) { }
		public RepoBuilder(string basePath) : this(basePath, Guid.NewGuid()) { }
		public RepoBuilder(string basePath, Guid unittestguid)
		{
			this.basePath = Path.Combine(basePath, $"kbgit\\{unittestguid}\\");
		}

		public KBGit BuildEmptyRepo()
		{
			Git = new KBGit(basePath);
			Git.InitializeRepository();
			return Git;
		}

		public RepoBuilder EmptyRepo()
		{
			Git = new KBGit(basePath);
			Git.InitializeRepository();
			return this;
		}

		public KBGit Build2Files3Commits()
		{
			Git = BuildEmptyRepo();

			AddFile("a.txt", "aaaaa");
			Git.Commit("Add a", "kasper", new DateTime(2017,1,1,1,1,1), Git.ScanFileSystem());

			AddFile("b.txt", "bbbb");
			Git.Commit("Add b", "kasper", new DateTime(2017, 2, 2, 2, 2, 2), Git.ScanFileSystem());

			AddFile("a.txt", "v2av2av2av2a");
			Git.Commit("Add a2", "kasper", new DateTime(2017, 3, 3, 3, 3, 3), Git.ScanFileSystem());

			return Git;
		}

		public RepoBuilder AddFile(string path) => AddFile(path, Guid.NewGuid().ToString());
		public RepoBuilder AddFile(string path, string content)
		{
			var filepath = Path.Combine(Git.CodeFolder, path);
			new FileInfo(filepath).Directory.Create();

			File.WriteAllText(filepath, content);
			return this;
		}

		public string ReadFile(string path)
		{
			return File.ReadAllText(Path.Combine(Git.CodeFolder, path));
		}

		public RepoBuilder DeleteFile(string path)
		{
			File.Delete(Path.Combine(basePath, path));
			return this;
		}

		public Id Commit() => Commit("Some message");

		public Id Commit(string message)
		{
			return Git.Commit(message, "author", DateTime.Now, Git.ScanFileSystem());
		}

		public RepoBuilder NewBranch(string branch)
		{
			Git.CreateBranch(branch);
			return this;
		}

		public RepoBuilder AddLocalHostRemote(int port)
		{
			Git.Hd.Remotes.Add(new Remote()
			{
				Name = "origin",
				Url = new Uri($"http://localhost:{port}")
			});
			return this;
		}
	}
}