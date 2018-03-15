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
    public class KbGitTests
    {
	    [Fact]
	    public void CommitWhenHeadless()
	    {
		    var repoBuilder = new RepoBuilder("reponame", @"c:\temp\");
		    var git = repoBuilder.Build2Files3Commits();
			git.CheckOut(git.HeadRef(1));
		    repoBuilder.AddFile("newfile", "dslfk");

		    var id = git.Commit("headless commit", "a", new DateTime(2010, 11, 12), git.ScanFileSystem());

			Assert.Equal("d9f76d36a423a4689a4f24d6f9d82e7804575a411d9897de36a4044e73c08b50", id.ToString());
	    }

	    [Fact]
	    public void When_Commit_a_similar_situation_to_a_previous_commit_Then_should_not_incread_blobs_nor_tree_blobs()
	    {
		    var repoBuilder = new RepoBuilder("reponame", @"c:\temp\");
		    var git = repoBuilder.BuildEmptyRepo();
		    repoBuilder.AddFile("a.txt", "aaaaa");
		    git.Commit("Add a", "kasper", new DateTime(2017, 1, 1, 1, 1, 1), git.ScanFileSystem());
			repoBuilder.AddFile("b.txt", "bbbb");
		    git.Commit("Add b", "kasper", new DateTime(2017, 2, 2, 2, 2, 2), git.ScanFileSystem());
			repoBuilder.DeleteFile("b.txt");
		    var blobCount = git.Hd.Blobs.Count;
		    var treeBlobCount = git.Hd.Trees.Count;

			git.Commit("deleted b", "arthur", new DateTime(2010, 11, 12), git.ScanFileSystem());

			Assert.Equal(blobCount, git.Hd.Blobs.Count);
			Assert.Equal(treeBlobCount, git.Hd.Trees.Count);
	    }

	    public string FileSystemScanFolder(KBGit git, string path)
	    {
		    return git.FileSystemScanFolder(path).ToString();
	    }

		[Fact]
	    public void Given_two_toplevel_files_Then_()
	    {
		    var repoBuilder = new RepoBuilder("", @"c:\temp\");
		    var git = repoBuilder.BuildEmptyRepo();
		    repoBuilder.AddFile("car.txt", "car");
		    repoBuilder.AddFile("door.txt", "door");

		    var files = FileSystemScanFolder(git, @"c:\temp\");

		    Assert.Equal(@"tree 2 
blob car.txt
blob door.txt",  files);
	    }

		[Fact]
		public void Given_two_files_in_subfolder_Then_()
	    {
		    var repoBuilder = new RepoBuilder("", @"c:\temp\");
		    var git = repoBuilder.BuildEmptyRepo();
		    repoBuilder.AddFile(@"FeatureVolvo\car.txt", "car");
		    repoBuilder.AddFile(@"FeatureVolvo\door.txt", "door");

		    var files = FileSystemScanFolder(git, @"c:\temp\");

		    Assert.Equal(
@"tree 1 
tree 2 FeatureVolvo
blob FeatureVolvo\car.txt
blob FeatureVolvo\door.txt", files);
		}

		[Fact]
		public void Get_folders_and_files()
	    {
		    var repoBuilder = new RepoBuilder("", @"c:\temp\");
		    var git = repoBuilder.BuildEmptyRepo();
		    repoBuilder.AddFile(@"FeatureVolvo\car.txt", "car");
		    repoBuilder.AddFile(@"FeatureGarden\tree.txt", "tree");
		    repoBuilder.AddFile(@"FeatureGarden\shovel.txt", "shovel");
		    repoBuilder.AddFile(@"FeatureGarden\Suburb\grass.txt", "grass");
		    repoBuilder.AddFile(@"FeatureGarden\Suburb\mover.txt", "mover");

		    var files = FileSystemScanFolder(git, @"c:\temp\");

		    Assert.Equal(
@"tree 2 
tree 3 FeatureGarden
blob FeatureGarden\shovel.txt
blob FeatureGarden\tree.txt
tree 2 FeatureGarden\Suburb
blob FeatureGarden\Suburb\grass.txt
blob FeatureGarden\Suburb\mover.txt
tree 1 FeatureVolvo
blob FeatureVolvo\car.txt"
			    , files);
	    }

	    [Fact]
	    public void Visit()
	    {
		    var repoBuilder = new RepoBuilder("", @"c:\temp\");
		    var git = repoBuilder.BuildEmptyRepo();
		    repoBuilder.AddFile(@"FeatureVolvo\car.txt", "car");
		    repoBuilder.AddFile(@"FeatureGarden\tree.txt", "tree");
		    repoBuilder.AddFile(@"FeatureGarden\shovel.txt", "shovel");
		    repoBuilder.AddFile(@"FeatureGarden\Suburb\grass.txt", "grass");
		    string buf = "";
		    git.FileSystemScanFolder(@"c:\temp\").Visit(x =>
			{
				if (x is TreeTreeLine t)
					buf += $"visittree {t.Path}\r\n";
				if (x is BlobTreeLine b)
					buf += $"visitblob {b.Path}\r\n";
			});

		    Assert.Equal(@"visittree 
visittree FeatureGarden
visitblob FeatureGarden\shovel.txt
visitblob FeatureGarden\tree.txt
visittree FeatureGarden\Suburb
visitblob FeatureGarden\Suburb\grass.txt
visittree FeatureVolvo
visitblob FeatureVolvo\car.txt
", buf);
	    }
	}

	public class KBGitHeadTests
	{
		[Fact]
		public void Given_fresh_repo_When_getting_headinfo_Then_fail()
		{
			var git = new RepoBuilder("reponame", @"c:\temp\").BuildEmptyRepo();

			Assert.Null(git.Hd.Head.GetId(git.Hd));
		}

		[Fact]
		public void Given_repo_When_getting_headinfo_Then_return_info()
		{
			var repoBuilder = new RepoBuilder("reponame", @"c:\temp\");
			var git = repoBuilder.BuildEmptyRepo();
			repoBuilder.AddFile("a.txt", "aaa");

			var firstId = git.Commit("b", "c", DateTime.Now);
			Assert.Equal(firstId, git.Hd.Head.GetId(git.Hd));
		}

		[Fact]
		public void Given_repo_When_getting_HeadRef_1_Then_return_parent_of_HEAD()
		{
			var git = new RepoBuilder("reponame", @"c:\temp").Build2Files3Commits();
			var parentOfHead = git.Hd.Commits[git.Hd.Head.GetId(git.Hd)].Parents.First();

			Assert.Equal(parentOfHead, git.HeadRef(1));
		}
	}


	public class GitCommitTests
	{
		[Fact]
		public void When_Commit_Then_content_is_stored()
		{
			var repoBuilder = new RepoBuilder("a", @"c:\temp\");
			var filename = "a.txt";
			var id1 = repoBuilder
				.EmptyRepo()
				.AddFile(filename, "version 1 a")
				.Commit();
			var id2 = repoBuilder
				.AddFile(filename, "version 2 a")
				.Commit();

			repoBuilder.Git.CheckOut(id1);
			Assert.Equal("version 1 a", repoBuilder.ReadFile(filename));
			repoBuilder.Git.CheckOut(id2);
			Assert.Equal("version 2 a", repoBuilder.ReadFile(filename));
		}
	}

	public class RepoBuilder
	{
		readonly string basePath;
		readonly string repositoryName;
		public KBGit Git;

		public RepoBuilder(string repositoryName, string basePath)
		{
			this.basePath = basePath;
			this.repositoryName = repositoryName;
		}

		public KBGit BuildEmptyRepo()
		{
			Git = new KBGit(repositoryName, basePath);
			Git.Init();
			return Git;
		}

		public RepoBuilder EmptyRepo()
		{
			Git = new KBGit(repositoryName, basePath);
			Git.Init();
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

		public RepoBuilder AddFile(string path, string content)
		{
			var filepath = Path.Combine(basePath, path);
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

		public Id Commit()
		{
			return Git.Commit("some message", "author", DateTime.Now, Git.ScanFileSystem());
		}
	}
}
