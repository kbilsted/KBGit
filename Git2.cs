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

        public Git2(string rootPath)
        {
            RootPath = rootPath;
            objectDb = new ObjectDb(rootPath);
        }

        public string Init()
        {
            new DirectoryInfo(Path.Combine(RootPath, ".git")).Create();
            new DirectoryInfo(Path.Combine(RootPath, ".git/refs/heads")).Create();

            File.WriteAllText(Path.Combine(RootPath, ".git/index"), "");
            File.WriteAllText(Path.Combine(RootPath, ".git/HEAD"), "ref: refs/heads/master");
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
            var hash = WriteIndexToFileSystem();
            return objectDb.WriteContent(@$"tree {hash}
parent nil
author {author} {now.Ticks} {now:zzz}
committer {author} {now.Ticks} {now:zzz}

{commitMessage}");
        }

        public string WriteIndexToFileSystem()
        {
            var lines = ReadIndex().Select(x => (hash: x.Substring(0, 64), path: x.Substring(64).Split('/').ToList()));

            return WriteIndexToFileSystem(lines);
        }

        private string WriteIndexToFileSystem(IEnumerable<(string hash, List<string> path)> lines)
        {
            var valueTuples = lines.ToArray();
            var fileLines = valueTuples.Where(x => x.path.Count == 1).Select(x => $"blob {x.hash}      {x.path.Single()}").ToArray();
            var folderLines = valueTuples.Where(x => x.path.Count > 1)
                .ToLookup(x => x.path.First())
                .Select(group => $"tree {WriteIndexToFileSystem(group.Select(x=>(hash:x.hash, path:x.path.GetRange(1,x.path.Count-1))))}     {group.Key}");
            return objectDb.WriteContent(string.Join("\r\n", fileLines.Concat(folderLines)));
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