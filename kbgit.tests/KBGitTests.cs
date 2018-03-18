using System;
using System.Collections.Generic;
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
		    repoBuilder
			    .EmptyRepo()
			    .AddFile("a.txt")
			    .Commit();

		    repoBuilder
			    .AddFile("b.txt")
			    .Commit();

		    Assert.Equal(2, repoBuilder.Git.Hd.Blobs.Count);
		    Assert.Equal(2, repoBuilder.Git.Hd.Commits.Count);

			repoBuilder
			    .DeleteFile("b.txt")
			    .Commit("deleted b");

		    Assert.Equal(2, repoBuilder.Git.Hd.Blobs.Count);
		    Assert.Equal(3, repoBuilder.Git.Hd.Commits.Count);
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
			Assert.Equal("reponame/master", git.Hd.Head.Branch);
		}

		[Fact]
		public void Given_repo_When_committing_getting_headinfo_Then_return_info()
		{
			var repoBuilder = new RepoBuilder("reponame", @"c:\temp\");
			var firstId = repoBuilder.EmptyRepo().AddFile("a.txt").Commit();

			Assert.Equal(firstId, repoBuilder.Git.Hd.Head.GetId(repoBuilder.Git.Hd));
			Assert.Null(repoBuilder.Git.Hd.Head.Id);
			Assert.Equal("reponame/master", repoBuilder.Git.Hd.Head.Branch);
		}

		[Fact]
		public void Given_repo_When_getting_HeadRef_1_Then_return_parent_of_HEAD()
		{
			var git = new RepoBuilder("reponame", @"c:\temp").Build2Files3Commits();
			var parentOfHead = git.Hd.Commits[git.Hd.Head.GetId(git.Hd)].Parents.First();

			Assert.Equal(parentOfHead, git.HeadRef(1));
		}

		[Fact]
		public void When_detached_head_and_commit_move_Then_update_head()
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
			var detachedId2=repoBuilder
				.AddFile("a.txt")
				.Commit();

			Assert.Null(repoBuilder.Git.Hd.Head.Branch);
			Assert.Equal(detachedId2, repoBuilder.Git.Hd.Head.Id);
			Assert.Equal(detachedId2, repoBuilder.Git.Hd.Head.GetId(repoBuilder.Git.Hd));
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
}
