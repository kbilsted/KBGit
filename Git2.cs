using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace KbgSoft.KBGit2
{
    public class Git2
    {
        public readonly string RootPath;
        readonly ObjectDb objectDb;
        readonly BranchHandling branchHandling;
        readonly HeadHandling headHandling;

        public Git2(string rootPath)
        {
            RootPath = rootPath;
            objectDb = new ObjectDb(rootPath);
            branchHandling = new BranchHandling(rootPath);
            headHandling = new HeadHandling(rootPath, branchHandling);
        }

        public string Init()
        {
            new DirectoryInfo(Path.Combine(RootPath, ".git")).Create();
            branchHandling.Init();
            headHandling.Init();
            File.WriteAllText(Path.Combine(RootPath, ".git/index"), "");
            objectDb.Init();
            return "Initialized empty Git repository";
        }

        /// <summary>
        /// stage a file
        /// https://mincong.io/2018/04/28/git-index/#:~:text=The%20index%20is%20a%20binary,Git%3A%20they%20are%20used%20interchangeably.
        /// </summary>
        public void Stage()
        {
            var stage = ReadIndex().Select(x => x.Split(' ')).ToDictionary(x => x[1], x => x[0]);

            Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories)
                .Select(x => new { fullPath = x, relPath = x.Substring(RootPath.Length).Replace('\\', '/') })
                .Where(x => !x.relPath.StartsWith(".git"))
                .ToList().ForEach(x => stage[x.relPath] = objectDb.WriteObjectFromFilepath(x.fullPath));

            WriteIndex(stage.OrderBy(x => x.Key).Select(x => $"{x.Value} {x.Key}"));
        }

        public string[] ReadIndex() => File.ReadAllLines(Path.Combine(RootPath, ".git/index"));

        public void WriteIndex(IEnumerable<string> content) => File.WriteAllLines(Path.Combine(RootPath, ".git/index"), content);

 
        public string Commit(string commitMessage, string author, DateTime now)
        {
            var parent = headHandling.IsHeadRef() && !branchHandling.Exists(headHandling.ReadHeadRef()) 
                ? "" 
                : $"\r\nparent {headHandling.ReadHead()}\r\n";

            var hash = WriteIndexToFileSystem();
            var commitId = objectDb.WriteContent(@$"tree {hash}{parent}
author {author} {now.Ticks} {now:zzz}
committer {author} {now.Ticks} {now:zzz}

{commitMessage}");

            headHandling.WriteHead(commitId);

            return commitId;
        }

        public string WriteIndexToFileSystem()
        {
            var lines = ReadIndex().Select(x => (hash: x.Substring(0, 64), path: x.Substring(64).Split('/').ToArray()));

            return WriteIndexToFileSystem(lines);
        }

        private string WriteIndexToFileSystem(IEnumerable<(string hash, string[] path)> lines)
        {
            var valueTuples = lines.ToArray();

            var fileLines = valueTuples.Where(x => x.path.Length == 1)
                .Select(x => $"blob {x.hash}      {x.path.Single()}");

            var folderLines = valueTuples.Where(x => x.path.Length > 1)
                .ToLookup(x => x.path.First(), x => (x.hash, path: x.path.Skip(1).ToArray()))
                .Select(x => $"tree {WriteIndexToFileSystem(x)}     {x.Key}");

            return objectDb.WriteContent(string.Join("\r\n", fileLines.Concat(folderLines)));
        }

        public string CreateBranch(string branchName)
        {
            return branchHandling.Checkout(branchName, headHandling.ReadHead()) == null
                ? $"Switched to branch '{branchName}'"
                : $"Switched to a new branch '{branchName}'";
        }

        /// <summary>
        /// return all branches and highlight current branch: "git branch"
        /// </summary>
        public string ListBranches()
        {
            return "";
        }

        public string CatFile(string hash) => objectDb.ReadObject(hash);
    }

    public class ObjectDb
    {
        public readonly string RootPath;

        public ObjectDb(string rootPath)
        {
            RootPath = rootPath;
        }

        public void Init() => new DirectoryInfo(Path.Combine(RootPath, ".git/objects")).Create();

        public string WriteContent(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = ByteHelper.ComputeSha(bytes);
            File.WriteAllBytes(Path.Combine(RootPath, $".git/objects/{hash}"), bytes);

            return hash;
        }

        public string WriteObjectFromFilepath(string path) => WriteContent(File.ReadAllText(path));

        public string ReadObject(string hash) => File.ReadAllText(Path.Combine(RootPath, $".git/objects/{hash}"));
    }

    public class BranchHandling
    {
        readonly string RootPath;

        public BranchHandling(string rootPath)
        {
            RootPath = rootPath;
        }

        public void Init() => Directory.CreateDirectory(GetFilePath(""));

        string GetFilePath(string branchName) => Path.Combine(RootPath, ".git/refs/heads/", branchName);

        void WriteToFile(string branchName, string hash)
        {
            if (branchName.Contains('/'))
                Directory.CreateDirectory(Path.GetDirectoryName(GetFilePath(branchName)));
            
            File.WriteAllText(GetFilePath(branchName), hash);
        }

        public string Checkout(string branchName, string? hash)
        {
            if (Exists(branchName))
                return ReadBranchHash(branchName);
            WriteToFile(branchName, hash);
            return null;
        }

        public bool Exists(string branchName) => File.Exists(GetFilePath(branchName));

        public string ReadBranchHash(string branchName) => File.ReadAllText(GetFilePath(branchName));

        public void WriteBranchHash(string branchName, string hash) => WriteToFile(branchName, hash);
    }

    public class HeadHandling
    {
        readonly string HeadFilePath;

        readonly BranchHandling branchHandling;

        public HeadHandling(string rootPath, BranchHandling branchHandling)
        {
            HeadFilePath = Path.Combine(rootPath, ".git/HEAD");

            this.branchHandling = branchHandling;
        }

        public void Init() => File.WriteAllText(HeadFilePath, "ref: refs/heads/master");

        public string ReadHead() => IsHeadRef() ? branchHandling.ReadBranchHash(ReadHeadRef()) : File.ReadAllText(HeadFilePath);

        public string ReadHeadRef() => File.ReadAllText(HeadFilePath).Substring("ref: refs/heads/".Length);

        public bool IsHeadRef() => File.ReadAllText(HeadFilePath).StartsWith("ref: ");

        public void WriteHead(string hash)
        {
            if (IsHeadRef())
                branchHandling.WriteBranchHash(ReadHeadRef(), hash);
            else File.WriteAllText(HeadFilePath, hash);
        }

        void WriteHeadChangeBranch(string branchName) => File.WriteAllText(HeadFilePath, $"ref: refs/heads/{branchName}");
    }

    public static class ByteHelper
    {
        static readonly SHA256 Sha = SHA256.Create();

        public static string ComputeSha(object o) => ComputeSha(Serialize(o));
        public static string ComputeSha(byte[] b) => string.Join("", Sha.ComputeHash(b).Select(x => $"{x:x2}"));

        public static byte[] Serialize(object o)
        {
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, o);
                stream.Seek(0, SeekOrigin.Begin);
                return stream.GetBuffer();
            }
        }

        public static T Deserialize<T>(Stream s) where T : class => (T)new BinaryFormatter().Deserialize(s);

        public static T Deserialize<T>(byte[] o) where T : class
        {
            using (var ms = new MemoryStream(o))
            {
                return (T)new BinaryFormatter().Deserialize(ms);
            }
        }
    }
}