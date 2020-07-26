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
            var pathAndHash = ReadIndex().Select(x => x.Split(' ')).ToDictionary(x => x[1], x => x[0].ToId());

            Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories)
                .Select(x => new { fullPath = x, relPath = x.Substring(RootPath.Length).Replace('\\', '/') })
                .Where(x => !x.relPath.StartsWith(".git"))
                .Each(x => pathAndHash[x.relPath] = objectDb.WriteObjectFromFilepath(x.fullPath));

            WriteIndex(pathAndHash.OrderBy(x => x.Key).Select(x => $"{x.Value} {x.Key}"));
        }

        public string[] ReadIndex() => File.ReadAllLines(Path.Combine(RootPath, ".git/index"));

        public void WriteIndex(IEnumerable<string> content) => File.WriteAllLines(Path.Combine(RootPath, ".git/index"), content);

        public Id WriteIndexToFileSystem()
        {
            var lines = ReadIndex().Select(x => (hash: x.Substring(0, 64).ToId(), path: x.Substring(64).Split('/').ToArray()));

            return WriteIndexToFileSystem(lines);
        }

        private Id WriteIndexToFileSystem(IEnumerable<(Id hash, string[] path)> lines)
        {
            var valueTuples = lines.ToArray();

            var fileLines = valueTuples.Where(x => x.path.Length == 1)
                .Select(x => new GitFileSystemLine() { Type = GitFileSystemLine.Kind.blob, Hash = x.hash, Name = x.path.Single() }.GenerateFileSystemLine());

            var folderLines = valueTuples.Where(x => x.path.Length > 1)
                .ToLookup(x => x.path.First(), x => (x.hash, path: x.path.Skip(1).ToArray()))
                .Select(x => new GitFileSystemLine() { Type = GitFileSystemLine.Kind.tree, Hash = WriteIndexToFileSystem(x), Name = x.Key }.GenerateFileSystemLine());

            return objectDb.WriteContent(fileLines.Concat(folderLines).StringJoin("\r\n"));
        }

        public Id Commit(string commitMessage, string author, DateTime now)
        {
            var commit = new Commit { Author = author, CommitMessage = commitMessage, Time = now, Content = WriteIndexToFileSystem() };

            if (headHandling.ReadHead() != "")
            {
                if (objectDb.ReadObject(headHandling.ReadHead()).Contains($"tree {commit.Content}"))
                    throw new Exception("nothing to commit, working tree clean");

                commit.Parents.Add(headHandling.ReadHead());
            }

            var commitId = objectDb.WriteContent(commit.CreateCommitNode());
            headHandling.WriteHead(commitId);

            return commitId;
        }

        public string CheckOutBranch(string branchName)
        {
            var result = branchHandling.Checkout(branchName, headHandling.ReadHead());
            headHandling.WriteHeadChangeBranch(branchName);

            ResetFolder();

            return $"Switched to a {(result == null ? "" : "new ")} branch '{branchName}'";
        }

        void ResetFolder()
        {
            Ext.Log("Reset folder");
            Directory.EnumerateDirectories(RootPath).Where(x => !x.EndsWith(".git")).Each(x => { Ext.Log($"delete '{x}'"); Directory.Delete(x, true); });
            Directory.EnumerateFiles(RootPath).Where(x => Ext.Log($"delete '{x}'")).Each(x => File.Delete(x));
            Id tree = new Commit().GetFilesEntry(objectDb.ReadObject(headHandling.ReadHead()));

            Restore(tree, RootPath);
        }

        void Restore(Id node, string path)
        {
            objectDb.ReadObject(node)
                .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
                .Each(x =>
            {
                Ext.Log($"Restoring {x}");
                var gfs = GitFileSystemLine.Parse(x);
                if (gfs.Type == GitFileSystemLine.Kind.tree)
                {
                    string dir = Path.Combine(path, $"/{gfs.Name}/");
                    Directory.CreateDirectory(dir);
                    Restore(gfs.Hash, dir);
                }
                else
                    File.WriteAllText(Path.Combine(path, gfs.Name), objectDb.ReadObject(gfs.Hash));
            });
        }

        /// <summary>
        /// return all branches and highlight current branch: "git branch"
        /// </summary>
        public string ListBranches()
        {
            if (headHandling.IsHeadRef())
                return branchHandling.ListBranches(headHandling.ReadHeadBranchName());
            return branchHandling.ListBranches(null);
        }

        public string? CatFile(string hash) => objectDb.ReadObject(hash.ToId());
    }

    class GitFileSystemLine
    {
        public enum Kind { tree, blob };
        public Kind Type;
        public string Name;
        public Id Hash;

        public static GitFileSystemLine Parse(string s)
        {
            var cols = s.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            return new GitFileSystemLine() { Type = cols[0] == "tree" ? Kind.tree : Kind.blob, Hash = cols[1].ToId(), Name = cols[2] };
        }

        public string GenerateFileSystemLine() => $"{Type} {Hash}      {Name}";
    }

    public class ObjectDb
    {
        public readonly string RootPath;

        public ObjectDb(string rootPath)
        {
            RootPath = rootPath;
        }

        public void Init() => Directory.CreateDirectory(Path.Combine(RootPath, ".git/objects/"));

        public Id WriteContent(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = ByteHelper.ComputeSha(bytes);
            File.WriteAllBytes(Path.Combine(RootPath, $".git/objects/{hash}"), bytes);

            return hash;
        }

        public Id WriteObjectFromFilepath(string path) => WriteContent(File.ReadAllText(path));

        public string? ReadObject(Id hash) => hash == "" ? null : File.ReadAllText(Path.Combine(RootPath, $".git/objects/{hash}"));
    }

    public class BranchHandling
    {
        readonly string RootPath;

        public BranchHandling(string rootPath)
        {
            RootPath = rootPath;
        }

        public void Init()
        {
            Directory.CreateDirectory(GetFilePath(""));
            WriteToFile("master", "");
        }

        string GetFilePath(string branchName) => Path.Combine(RootPath, ".git/refs/heads/", branchName);

        void WriteToFile(string branchName, string hash)
        {
            if (branchName.Contains('/'))
                Directory.CreateDirectory(Path.GetDirectoryName(GetFilePath(branchName)));

            File.WriteAllText(GetFilePath(branchName), hash);
        }

        public string? Checkout(string branchName, string? hash)
        {
            if (Exists(branchName))
                return ReadBranchHash(branchName);
            WriteToFile(branchName, hash.ToString());
            return null;
        }

        public bool Exists(string branchName) => File.Exists(GetFilePath(branchName));

        public Id ReadBranchHash(string branchName) => File.ReadAllText(GetFilePath(branchName)).ToId();

        public void WriteBranchHash(string branchName, Id hash) => WriteToFile(branchName, hash);

        public string ListBranches(string? headName)
        {
            var branches = Directory.GetFiles(GetFilePath(""), "*", SearchOption.AllDirectories)
                .Select(x => x.Substring(GetFilePath("").Length))
                .OrderBy(x => x)
                .Select(x => $"{(x == headName ? '*' : ' ')} {x}");

            var detached = headName == null ? $"\r\n* (HEAD detached at {headName})\r\n" : "";

            return $"{detached}{branches.StringJoin("\r\n")}";
        }
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

        public Id ReadHead() => IsHeadRef() ? branchHandling.ReadBranchHash(ReadHeadBranchName()) : File.ReadAllText(HeadFilePath).ToId();

        public string ReadHeadBranchName() => File.ReadAllText(HeadFilePath).Substring("ref: refs/heads/".Length);

        public bool IsHeadRef() => File.ReadAllText(HeadFilePath).StartsWith("ref: ");

        public void WriteHead(Id hash)
        {
            if (IsHeadRef())
                branchHandling.WriteBranchHash(ReadHeadBranchName(), hash);
            else File.WriteAllText(HeadFilePath, hash);
        }

        public void WriteHeadChangeBranch(string branchName) => File.WriteAllText(HeadFilePath, $"ref: refs/heads/{branchName}");
    }

    public static class ByteHelper
    {
        static readonly SHA256 Sha = SHA256.Create();

        public static string ComputeSha(object o) => ComputeSha(Serialize(o));
        public static Id ComputeSha(byte[] b) => Sha.ComputeHash(b).Select(x => $"{x:x2}").StringJoin().ToId();

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

    public class Commit
    {
        public List<Id> Parents = new List<Id>();
        public Id Content;
        public string CommitMessage;
        public string Author;
        public DateTime Time;

        public string CreateCommitNode()
        {
            return @$"tree {Content}{Parents.Select(x => $"\r\nparent {x}").StringJoin()}
author {Author} {Time.Ticks} {Time:zzz}
committer {Author} {Time.Ticks} {Time:zzz}

{CommitMessage}";
        }

        public Id GetFilesEntry(string commitNode)
        {
            if (commitNode.StartsWith("tree "))
                return commitNode.Substring(5, 64).ToId();
            throw new Exception($"Not a valid commit node:\n{commitNode}");
        }
    }

    public class Id
    {
        public string Value = "";

        public Id(string value)
        {
            Value = value;
        }

        public static implicit operator string(Id ts) => ts?.Value;

        public override string ToString() => Value;
    }


    public class Differ
    {
        public List<(string line, int lineNo)> Diff(string[] a, string[] b)
        {
            return Diff(a.Select((x, i) => (x, i + 1)), b.Select((x, i) => (x, i + 1))).ToList();
        }

        public string ToString(List<(string line, int lineNo)> r)
        {
            return r.Select(x => string.Join($"\r\n", x.line)).StringJoin("\r\n");
        }

        IEnumerable<(string line, int lineNo)> Diff(IEnumerable<(string line, int lineNo)> fileA, IEnumerable<(string line, int lineNo)> fileB)
        {
            (string line, int lineNo)[] a = fileA.ToArray(), b = fileB.ToArray();
            int longestOverlap = 0, offsetA = 1, offsetB = -1, overlap;
            for (int ia = 0; ia < a.Length; ia++)
            {
                for (int ib = 0; ib < b.Length; ib++)
                {
                    for (overlap = 0;
                        ia + overlap < a.Length && ib + overlap < b.Length && a[ia + overlap].line == b[ib + overlap].line;
                        overlap++) ;

                    if (overlap > longestOverlap)
                    {
                        longestOverlap = overlap; offsetA = ia; offsetB = ib;
                    }
                }
            }

            return (longestOverlap == 0)
                ? fileA.Select(x => ($"-{x.line}", x.lineNo))
                  .Concat(fileB.Select(x => ($"+{x.line}", x.lineNo)))
                : Diff(fileA.Take(offsetA), fileB.Take(offsetB))
                  .Concat(Diff(fileA.Skip(offsetA + longestOverlap), fileB.Skip(offsetB + longestOverlap)));
        }
    }


    public static class Ext
    {
        public static Id ToId(this string s) => new Id(s);
        public static string StringJoin(this IEnumerable<string> col, string separator = "") => string.Join(separator, col);
        public static void Each<T>(this IEnumerable<T> col, Action<T> code) => col.ToList().ForEach(code);
        public static bool Log(string s)
        {
            Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine(s); Console.ForegroundColor = ConsoleColor.White;
            return true;
        }
    }
}