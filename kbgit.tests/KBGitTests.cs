using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using KbgSoft.KBGit;
using Xunit;
using Xunit.Abstractions;

namespace kbgit.tests
{
    public class KBGitTests
    {
	    private readonly ITestOutputHelper output;

		public KBGitTests(ITestOutputHelper output)
		{
			this.output = output;
		}
	    [Fact]
	    public void CommitWhenHeadless()
	    {
		    var repoBuilder = new RepoBuilder("reponame", @"c:\temp\");
		    var git = repoBuilder.BuildStandardRepo();
			git.CheckOut(git.HeadRef(1));
		    repoBuilder.AddFile("newfile", "dslfk");

		    var id = git.Commit("headless commit", "a", new DateTime(2010, 11, 12), git.ScanFileSystem());

			Assert.Equal("80f6435892b2757c08921ded9ed454846f6964933991fc4ded1f3296e338f316", id.ToString());
	    }

	    [Fact]
	    public void When_Commit_a_similar_situation_to_a_previous_commit_Then_should_not_incread_blobs_nor_tree_blobs()
	    {
			StreamWriter sw = new StreamWriter(@"c:\src\out.txt") {AutoFlush = true};
		    Console.SetOut(sw);

		    var repoBuilder = new RepoBuilder("reponame", @"c:\temp\");
		    var git = repoBuilder.BuildStandardRepo();
		    repoBuilder.DeleteFile("b.txt");
		    var blobCount = git.Hd.Blobs.Count;
		    var treeBlobCount = git.Hd.Trees.Count;

			git.Commit("deleted b", "arthur", new DateTime(2010, 11, 12), git.ScanFileSystem());

			Assert.Equal(blobCount, git.Hd.Blobs.Count);
			Assert.Equal(treeBlobCount, git.Hd.Trees.Count);
	    }
	}

	public class KBGitHeadTests
	{
		[Fact]
		public void Given_fresh_repo_When_getting_headinfo_Then_fail()
		{
			var git = new KBGit("reponame", @"c:\temp\");
			git.Init();

			var id = git.Hd.Head.GetId(git.Hd);

			Assert.Null(id);
		}

		[Fact]
		public void Given_repo_When_getting_headinfo_Then_return_info()
		{
			var git = new RepoBuilder("reponame", @"c:\temp\").BuildStandardRepo();

			var id = git.Hd.Head.GetId(git.Hd);

			Assert.Equal("d0843b87b01755eb213325fdcd26956fba7df004b90cba17afca44f5f80d7a80", id.ToString());
		}

		[Fact]
		public void Given_repo_When_getting_HeadRef_1_Then_return_parent_of_HEAD()
		{
			var git = new RepoBuilder("reponame", @"c:\temp").BuildStandardRepo();

			var id = git.HeadRef(1);
			var parentOfHead = git.Hd.Commits[git.Hd.Head.GetId(git.Hd)].Parents.First();

			Assert.Equal(parentOfHead.ToString(), id.ToString());
		}
	}

	public class RepoBuilder
	{
		readonly string basePath;
		readonly string repositoryName;

		public RepoBuilder(string repositoryName, string basePath)
		{
			this.basePath = basePath;
			this.repositoryName = repositoryName;
		}

		public KBGit BuildStandardRepo()
		{
			var git = new KBGit(repositoryName, basePath);
			git.Init();

			AddFile("a.txt", "aaaaa");
			git.Commit("Add a", "kasper", new DateTime(2018,1,1,1,1,1), git.ScanFileSystem());

			AddFile("b.txt", "bbbb");
			git.Commit("Add b", "kasper", new DateTime(2017, 2, 2, 2, 2, 2), git.ScanFileSystem());

			return git;
		}

		public RepoBuilder AddFile(string path, string content)
		{
			File.WriteAllText(Path.Combine(basePath, path), content);
			return this;
		}

		public RepoBuilder DeleteFile(string path)
		{
			File.Delete(Path.Combine(basePath, path));
			return this;
		}
	}
}
