using System;
using System.IO;
using FluentAssertions;
using KbgSoft.KBGit2;
using Xunit;

namespace kbgit.tests2
{
    public class Git2Tests
    {
        private readonly RepoBuilder builder = new RepoBuilder();


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
            git.CatFile("9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0").Should().Be("aaa");
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

            var commitHash = git.Commit("commit message", "Clarke Kent", new DateTime(2020, 1, 2, 3, 4, 5));
            git.CatFile(commitHash).Should().Be(@$"tree a06f8f314a671df7ea4ffd0c1d98c69b6d84fae50bf0ff3e02d60cf3564f6d23
author Clarke Kent 637135310450000000 +01:00
committer Clarke Kent 637135310450000000 +01:00

commit message");

            git.CatFile("a06f8f314a671df7ea4ffd0c1d98c69b6d84fae50bf0ff3e02d60cf3564f6d23").Should()
                .Be("blob 9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0       a.txt");
            git.CatFile("9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0").Should()
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

            var commitHash = git.Commit("commit message", "Clarke Kent", new DateTime(2020, 1, 2, 3, 4, 5));
            git.CatFile(commitHash).Should().Be(@$"tree 64af0aefbec30845dddd56643c23e9148758d8fbd34fe741f0643cb33c8abfca
author Clarke Kent 637135310450000000 +01:00
committer Clarke Kent 637135310450000000 +01:00

commit message");

            // show root commit dir
            git.CatFile("64af0aefbec30845dddd56643c23e9148758d8fbd34fe741f0643cb33c8abfca").Should().Be(
@"blob 9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0       a.txt
tree b70a5f51331663558c5326803e159cb010287388e9a8fb8ecdd36b6565c41182      bar
tree f159e019c62b0cb3c7f089e7dc229cd81d10feeac3bb8f39b9250f9c98528d4d      foo");
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

            var commitHash = git.Commit("commit message", "Clarke Kent", new DateTime(2020, 1, 2, 3, 4, 5));

            indexBefore.Should().Be(builder.ReadHEAD());
        }


        [Fact]
        public void When_comitting_Then_branch_is_updated()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .Stage()
                .Build();

            var commitHash = git.Commit("commit message", "Clarke Kent", new DateTime(2020, 1, 2, 3, 4, 5));

            builder.ReadBranch("master").Should().Be(commitHash);
                    }

        [Fact]
        public void When_list_branches_Then_return_empty()
        {
            var git = builder.InitEmptyRepo().Build();

            new DirectoryInfo(Path.Combine(git.RootPath, ".git")).Exists.Should().BeTrue();
        }
    }

    class RepoBuilder
    {
        public string TestPath = $@"c:\temp\kbggit{DateTime.Now.Ticks}\";

        private Git2 Git;

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


        public RepoBuilder Commit()
        {
            Git.Commit("A commit message", "Clark Kent", new DateTime(2020, 1, 2, 3, 4, 5));
            return this;
        }

        public void ChangeFile(string path, string newContent)
        {
            File.WriteAllText(Path.Combine(TestPath, path), newContent);
        }

        public string ReadHEAD()
        {
            return File.ReadAllText(Path.Combine(TestPath, ".git/HEAD"));
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
