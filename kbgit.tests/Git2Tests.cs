using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using KbgSoft.KBGit2;
using Xunit;

namespace kbgit.tests2
{
    public class Git2Tests
    {
        private readonly RepoBuilder builder = new RepoBuilder();
        readonly DateTime date = new DateTime(2020, 1, 2, 3, 4, 5);

        [Fact]
        public void When_init_Then_create_git_folder()
        {
            var git = builder.MakeEmptyRepo().Build();

            var msg = git.Init();

            msg.Should().Be("Initialized empty Git repository");
            new DirectoryInfo(Path.Combine(git.RootPath, ".git")).Exists.Should().BeTrue();
            new DirectoryInfo(Path.Combine(git.RootPath, ".git/refs/heads")).Exists.Should().BeTrue();
            new DirectoryInfo(Path.Combine(git.RootPath, ".git/objects")).Exists.Should().BeTrue();
            File.Exists(Path.Combine(git.RootPath, ".git/index")).Should().BeTrue();
            File.ReadAllText(Path.Combine(git.RootPath, ".git/HEAD")).Should().Be("ref: refs/heads/master");
        }

        [Fact]
        public void When_empty_repo_Then_index_is_empty()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa").Build();

            git.ReadIndex().Should().BeEquivalentTo(new string[0]);
        }

        [Fact]
        public void When_staging_file_Then_added_to_the_index_and_stored_in_objectstore()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa").Build();

            git.Stage();

            git.ReadIndex().Should().BeEquivalentTo("9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0 a.txt");
            git.CatFile("9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0".ToId()).Should().Be("aaa");
        }

        [Fact]
        public void When_restaging_a_changed_file_Then_rehash_it()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .Build();

            git.Stage();
            builder.ChangeFile("a.txt", "bbb");
            git.Stage();

            git.ReadIndex().Should().BeEquivalentTo(
                "3e744b9dc39389baf0c5a0660589b8402f3dbb49b89b3e75f2c9355852a3c677 a.txt");
        }

        [Fact]
        public void When_staging_add_all_files_Then_add_to_index()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .WithFile("b.txt", "bbb")
                .Build();

            git.Stage();

            git.ReadIndex().Should().BeEquivalentTo(
                "9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0 a.txt",
                "3e744b9dc39389baf0c5a0660589b8402f3dbb49b89b3e75f2c9355852a3c677 b.txt"
                );
        }

        [Fact]
        public void When_staging_files_in_sub_folders_Then_add_to_index()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .WithFile("foo/b.txt", "bbb")
                .WithFile("foo/d.txt", "bbb")
                .WithFile("bar/c.txt", "ccc")
                .Build();

            git.Stage();

            git.ReadIndex().Should().BeEquivalentTo(
                "9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0 a.txt",
                "64daa44ad493ff28a96effab6e77f1732a3d97d83241581b37dbd70a7a4900fe bar/c.txt",
                "3e744b9dc39389baf0c5a0660589b8402f3dbb49b89b3e75f2c9355852a3c677 foo/b.txt",
                "3e744b9dc39389baf0c5a0660589b8402f3dbb49b89b3e75f2c9355852a3c677 foo/d.txt"
            );
        }

        [Fact]
        public void When_comitting_file_Then_store_commit_info_and_fileinfo()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .Stage()
                .Build();

            var commitHash = git.Commit("commit message", "Clarke Kent", date);
            git.CatFile(commitHash).Should().Be(@$"tree a06f8f314a671df7ea4ffd0c1d98c69b6d84fae50bf0ff3e02d60cf3564f6d23
author Clarke Kent 637135310450000000 +01:00
committer Clarke Kent 637135310450000000 +01:00

commit message");

            git.CatFile("a06f8f314a671df7ea4ffd0c1d98c69b6d84fae50bf0ff3e02d60cf3564f6d23".ToId()).Should()
                .Be("blob 9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0       a.txt");
            git.CatFile("9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0".ToId()).Should()
                .Be("aaa");
        }

        [Fact]
        public void When_comitting_subfolder_files_Then_store_commit_info_and_fileinfo()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .WithFile("foo/b.txt", "bbb")
                .WithFile("foo/d.txt", "bbb")
                .WithFile("bar/c.txt", "ccc")
                .Stage()
                .Build();

            var commitHash = git.Commit("commit message", "Clarke Kent", date);
            git.CatFile(commitHash).Should().Be(@$"tree a04ab901b141d9edbb299852f44dc8e35c3873dd1f912073e40351b3bd3abed9
author Clarke Kent 637135310450000000 +01:00
committer Clarke Kent 637135310450000000 +01:00

commit message");

            // show root commit dir
            git.CatFile("a04ab901b141d9edbb299852f44dc8e35c3873dd1f912073e40351b3bd3abed9").Should().Be(
@"blob 9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0       a.txt
tree b70a5f51331663558c5326803e159cb010287388e9a8fb8ecdd36b6565c41182       bar
tree f159e019c62b0cb3c7f089e7dc229cd81d10feeac3bb8f39b9250f9c98528d4d       foo");
            // show 'foo'
            git.CatFile("f159e019c62b0cb3c7f089e7dc229cd81d10feeac3bb8f39b9250f9c98528d4d").Should().Be(
@"blob 3e744b9dc39389baf0c5a0660589b8402f3dbb49b89b3e75f2c9355852a3c677      b.txt
blob 3e744b9dc39389baf0c5a0660589b8402f3dbb49b89b3e75f2c9355852a3c677      d.txt");
            // show 'bar'
            git.CatFile("b70a5f51331663558c5326803e159cb010287388e9a8fb8ecdd36b6565c41182").Should().Be(
                @"blob 64daa44ad493ff28a96effab6e77f1732a3d97d83241581b37dbd70a7a4900fe      c.txt");
        }

        [Fact]
        public void When_comitting_file_Then_index_is_unchanged()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .Stage()
                .Build();
            var indexBefore = builder.ReadHEAD();

            git.Commit("commit message", "Clarke Kent", date);

            indexBefore.Should().Be(builder.ReadHEAD());
        }

        [Fact]
        public void When_comitting_Then_branch_is_updated()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .Stage()
                .Build();

            var commitHash = git.Commit("commit message", "Clarke Kent", date);

            builder.ReadBranch("master").Should().Be(commitHash);
        }

        [Fact]
        public void When_comitting_with_nothing_staged_Then_then_fail()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .Stage()
                .Commit()
                .ChangeFile("a.txt", "bbb")
                .Build();

            Action code = () => git.Commit("message 2", "Clarke", date);

            code.Should().Throw<Exception>().WithMessage("nothing to commit, working tree clean");
        }


        [Fact]
        public void When_comitting_Then_the_parent_is_referred_in_commit()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .StageCommit()
                .ChangeFile("a.txt", "bbb")
                .Stage()
                .Build();

            var commitHash2 = git.Commit("message 2", "Clarke", date);

            git.CatFile(commitHash2)
                .Should().Contain($"parent {builder.Commits.Single()}")
                .And.Contain("message 2");
            builder.ReadBranch("master").Should().Be(commitHash2);
        }

        [Fact]
        public void When_only_master_branch_and_list_branches_Then_return_master()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .StageCommit()
                .Build();

            git.ListBranches().Should().Be("* master");
        }

        [Fact]
        public void When_checkingout_branch_b_Then_list_all_branched_with_focus_on_b()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .StageCommit()
                .CheckoutBranch("b")
                .Build();

            git.ListBranches().Should().Be(
@"* b
  master");
        }

        [Fact]
        public void When_checkingout_branch_Then_reset_source_folder()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .StageCommit()
                .CheckoutBranch("b")
                .ChangeFile("a.txt", "changed")
                .WithFile("c.txt", "new file")
                .StageCommit()
                .CheckoutBranch("master")
                .Build();

            Directory.GetFiles(builder.TestPath).Select(x => x.Substring(builder.TestPath.Length)).Should().BeEquivalentTo(new[] { "a.txt" });

            File.ReadAllText(Path.Combine(builder.TestPath, "a.txt")).Should().Be("aaa");
        }


        [Fact]
        public void When_logging_Then_list_commit()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .StageCommit(@"first")
                .Build();

            git.Log().Should().Be(
@"first
");
        }

        [Fact]
        public void When_logging_Then_list_commits()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .StageCommit("first")
                .WithFile("b.txt", "bbb")
                .StageCommit("second")
                .Build();

            git.Log().Should().Be(
@"second
first
");
        }
    }

    public class DiffTests
    {
        Differ differ = new Differ();

        [Fact]
        public void When_diffing_identical_files_Then_report_no_differences()
        {
            differ.Diff(@"
a
b
c".Split("\r\n"),
@"
a
b
c".Split("\r\n")).Should().BeEmpty();

        }

        [Fact]
        public void When_diffing_files_Then_report_differences()
        {
            var diffs = differ.Diff(@"
a
b
b
b
c".Split("\r\n"),
@"
a
b
c
c".Split("\r\n"));

            differ.ToString(diffs).Should().Be(
@"-b
-b
+c");
        }
        [Fact]
        public void When_diffing_completely_different_files_Then_report_differences()
        {
            var diffs = differ.Diff(@"
a
b
c".Split("\r\n"),
@"
d
e
f".Split("\r\n"));

            differ.ToString(diffs).Should().Be(
@"-a
-b
-c
+d
+e
+f");
        }
    }

    class RepoBuilder
    {
        public string TestPath = $@"c:\temp\kbggit\{DateTime.Now.Ticks}\";

        Git2 Git;

        public List<Id> Commits = new List<Id>();

        public RepoBuilder MakeEmptyRepo()
        {
            Directory.CreateDirectory(TestPath);
            Git = new Git2(TestPath);
            return this;
        }

        public RepoBuilder InitEmptyRepo()
        {
            MakeEmptyRepo();
            Git.Init();
            return this;
        }

        public RepoBuilder WithFile(string path, string content)
        {
            var filePath = Path.Combine(TestPath, path);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, content);
            return this;
        }

        public RepoBuilder Stage()
        {
            Git.Stage();
            return this;
        }


        public RepoBuilder Commit(string msg = "A commit message")
        {
            Commits.Add(Git.Commit(msg, "Clark Kent", new DateTime(2020, 1, 2, 3, 4, 5)));
            return this;
        }

        public RepoBuilder StageCommit(string msg="A commit message")
        {
            Stage();
            return Commit(msg);
        }

        public RepoBuilder ChangeFile(string path, string newContent)
        {
            File.WriteAllText(Path.Combine(TestPath, path), newContent);
            return this;
        }

        public string ReadHEAD()
        {
            return File.ReadAllText(Path.Combine(TestPath, ".git/HEAD"));
        }

        public RepoBuilder CheckoutBranch(string name)
        {
            Git.CheckOutBranch(name);
            return this;
        }

        public string ReadBranch(string branchName)
        {
            return File.ReadAllText(Path.Combine(TestPath, ".git/refs/heads/", branchName));
        }
        public Git2 Build()
        {
            return Git;
        }
    }
}
