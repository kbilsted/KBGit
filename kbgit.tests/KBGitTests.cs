using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KbgSoft.KBGit;
using Xunit;

namespace kbgit.tests
{
    public class KbGitTests
    {
	    [Fact]
	    public void CommitWhenHeadless()
	    {
		    var repoBuilder = new RepoBuilder("reponame", @"c:\temp\");
		    var git = repoBuilder.Build2Files3Commits();
			git.Checkout(git.HeadRef(1));
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

		public string FileSystemScanFolder(KBGit git)
		{
			return git.FileSystemScanFolder(git.CodeFolder).ToString();
		}

		[Fact]
	    public void Given_two_toplevel_files_Then_()
	    {
		    var repoBuilder = new RepoBuilder("", @"c:\temp\");
		    var git = repoBuilder.BuildEmptyRepo();
		    repoBuilder.AddFile("car.txt", "car");
		    repoBuilder.AddFile("door.txt", "door");

			var files = FileSystemScanFolder(git);

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

			var files = FileSystemScanFolder(git);

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

			var files = FileSystemScanFolder(git);

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
			git.FileSystemScanFolder(git.CodeFolder).Visit(x =>
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

			repoBuilder.Git.Checkout(id1);
			Assert.Equal("version 1 a", repoBuilder.ReadFile(filename));
			repoBuilder.Git.Checkout(id2);
			Assert.Equal("version 2 a", repoBuilder.ReadFile(filename));
		}

		[Fact]
		public void When_branching_and_commit_and_update_back_Then_reset_content_to_old_branch()
		{
			var repoBuilder = new RepoBuilder("a", @"c:\temp\");
			repoBuilder
				.EmptyRepo()
				.AddFile("a.txt", "version 1 a")
				.Commit();
			repoBuilder.NewBranch("featurebranch")
				.AddFile("b.txt", "version 1 b")
				.Commit();
			IEnumerable<string> FilesInRepo() => repoBuilder.Git.ScanFileSystem().Select(x => x.Path);

			Assert.Equal(new[] {"a.txt", "b.txt"}, FilesInRepo());

			repoBuilder.Git.Checkout("master");
			Assert.Equal(new[] { "a.txt" }, FilesInRepo());

			repoBuilder.Git.Checkout("featurebranch");
			Assert.Equal(new[] { "a.txt", "b.txt" }, FilesInRepo());
		}

		[Fact]
		public void When_detached_head_Then_git_branches_shows_detached_as_branch()
		{
			var repoBuilder = new RepoBuilder("a", @"c:\temp\");
			var detachedId = repoBuilder
				.EmptyRepo()
				.AddFile("a.txt")
				.Commit();
			repoBuilder
				.AddFile("b.txt")
				.Commit();

			repoBuilder.Git.Checkout(detachedId);

			Assert.Equal($@"* (HEAD detached at {detachedId.ToString().Substring(0, 7)})
  a/master", repoBuilder.Git.Branch());
		}

		[Fact]
		public void When_branching_Then_Branchinfo_show_new_branchname()
		{
			var repoBuilder = new RepoBuilder("a", @"c:\temp\");
			repoBuilder
				.EmptyRepo()
				.AddFile("a.txt")
				.Commit();
			Assert.Equal("* a/master", repoBuilder.Git.Branch());

			repoBuilder.NewBranch("featurebranch");
			Assert.Equal(@"* a/featurebranch
  a/master", repoBuilder.Git.Branch());
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

		public Id Commit()
		{
			return Git.Commit("some message", "author", DateTime.Now, Git.ScanFileSystem());
		}

		public RepoBuilder NewBranch(string branch)
		{
			Git.CheckOut_b(branch);
			return this;
		}
	}
}
