using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KbgSoft.KBGit2
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
            new DirectoryInfo(Path.Combine(RootPath, ".git/objects")).Create();
            File.WriteAllText(Path.Combine(RootPath, ".git/index"), "");
            File.WriteAllText(Path.Combine(RootPath, ".git/HEAD"), "ref: refs/heads/master");
            return "Initialized empty Git repository";
        }

        /// <summary>
        /// stage a file
        /// https://mincong.io/2018/04/28/git-index/#:~:text=The%20index%20is%20a%20binary,Git%3A%20they%20are%20used%20interchangeably.
        /// </summary>
        /// <param name="pattern"></param>
        public void Stage()
        {
            var stage = Directory.EnumerateFiles(RootPath)
                .ToDictionary(x => x.Substring(RootPath.Length), x => WriteObjectToObjectStore(x));
            ReadIndex()
                .Select(x => x.Split(' '))
                .Where(x=> !stage.ContainsKey(x[1]))
                .ToList().ForEach(x => stage.Add(x[1], x[0]));

            WriteIndex(stage.OrderBy(x => x.Key)
                .Select(x => $"{x.Value} {x.Key}"));
        }

        public string[] ReadIndex() => File.ReadAllLines(Path.Combine(RootPath, ".git/index"));
        public void WriteIndex(IEnumerable<string> content) => File.WriteAllLines(Path.Combine(RootPath, ".git/index"), content);

        public string WriteObjectToObjectStore(string path)
        {
            var content = File.ReadAllBytes(path);
            var hash = ByteHelper.ComputeSha(content);
            File.WriteAllBytes(Path.Combine(RootPath, $".git/objects/{hash}"), content);
            return hash;
        }

        public string ReadObjectFromObjectStore(string hash) => File.ReadAllText(Path.Combine(RootPath, $".git/objects/{hash}"));

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

    public static class ByteHelper
    {
        static readonly SHA256 Sha = SHA256.Create();

        public static string ComputeSha(object o) => ComputeSha(Serialize(o));
        public static string ComputeSha(byte[] b) => string.Join("", Sha.ComputeHash(b).Select(x => String.Format("{0:x2}", x)));

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