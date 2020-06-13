using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KbgSoft.KBGit
{
    public class Git2
    {
        public readonly string RootPath;

        public Git2(string rootPath)
        {
            RootPath = rootPath;
        }

        public string Init()
        {
            new DirectoryInfo(Path.Combine(RootPath, ".git")).Create();
            new DirectoryInfo(Path.Combine(RootPath, ".git", "refs", "heads")).Create();
            new DirectoryInfo(Path.Combine(RootPath, ".git", "objects")).Create();
            new DirectoryInfo(Path.Combine(RootPath, ".git", "index")).Create();
            File.WriteAllText(Path.Combine(RootPath, ".git", "HEAD"), "ref: refs/heads/master");
            return "Initialized empty Git repository";
        }

        public string Commit(string commitMessage)
        {
            return "";
        }

        public string CreateBranch(string name)
        {
            return "";
        }

        /// <summary>
        /// return all branches and highlight current branch: "git branch"
        /// </summary>
        public string ListBranches()
        {
            return "";
        }
    }

}