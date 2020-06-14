using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using KbgSoft.KBGit;
using KbgSoft.KBGit2;
using Xunit;

namespace kbgit.tests2
{
    public class Git2Tests
    {
        private RepoBuilder builder = new RepoBuilder();


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

            git.Add("*");

            git.ReadIndex().Should().BeEquivalentTo("9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0 a.txt");
            git.ReadObjectFromObjectStore("9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0").Should().Be("aaa");
        }

        [Fact]
        public void When_staging_add_all_files_Then_add_to_index()
        {
            var git = builder.InitEmptyRepo()
                .WithFile("a.txt", "aaa")
                .WithFile("b.txt", "aaa")
                .Build();

            git.Add("*");

            git.ReadIndex().Should().BeEquivalentTo(
                "9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0 a.txt",
                "9834876dcfb05cb167a5c24953eba58c4ac89b1adf57f28f2f9d09af107ee8f0 b.txt"
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
        string name = $@"c:\temp\kbggit{DateTime.Now.Ticks}\";

        private Git2 Git;

        public RepoBuilder MakeEmptyRepo()
        {
                Directory.CreateDirectory(name);
                Git = new Git2(name);
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
                File.WriteAllText(Path.Combine(name,path), content);
            return this;
        }

        public Git2 Build()
        {
            return Git;
        }

    }

}
