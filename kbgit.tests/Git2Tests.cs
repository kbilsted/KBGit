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
            git.ReadObjectFromObjectStore("9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0").Should().Be("aaa");
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
                File.WriteAllText(Path.Combine(TestPath,path), content);
            return this;
        }

        public void ChangeFile(string path, string newContent)
        {
            File.WriteAllText(Path.Combine(TestPath,path), newContent);
        }

        public Git2 Build()
        {
            return Git;
        }
    }
}
