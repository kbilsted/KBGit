using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using KbgSoft.KBGit;
using Xunit;

namespace kbgit.tests
{
    public class Git2Tests
    {
        Git2 MakeEmptyRepo()
        {
            var name = $@"c:\temp\kbggit{DateTime.Now.Ticks}";
            Directory.CreateDirectory(name);
            return new Git2(name);
        }

        Git2 InitEmptyRepo()
        {
            var git = MakeEmptyRepo();
            git.Init();
            return git;
        }

        [Fact]
        public void When_init_Then_create_git_folder()
        {
            var git = MakeEmptyRepo();
            
            var msg = git.Init();
                
            msg.Should().Be("Initialized empty Git repository");
            new DirectoryInfo(Path.Combine(git.RootPath, ".git")).Exists.Should().BeTrue();
            new DirectoryInfo(Path.Combine(git.RootPath, ".git/refs/heads")).Exists.Should().BeTrue();
            new DirectoryInfo(Path.Combine(git.RootPath, ".git/objects")).Exists.Should().BeTrue();
            new DirectoryInfo(Path.Combine(git.RootPath, ".git/index")).Exists.Should().BeTrue();
            File.ReadAllText(Path.Combine(git.RootPath, ".git/HEAD")).Should().Be("ref: refs/heads/master");
        }

        [Fact]
        public void When_list_branches_Then_return_empty()
        {
            var git = InitEmptyRepo();

            new DirectoryInfo(Path.Combine(git.RootPath, ".git")).Exists.Should().BeTrue();
        }


    }
}
