using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace KbgSoft.KBGit
{
	public static class Sha
	{
		static readonly SHA256 sha = SHA256.Create();

		public static byte[] Compute(object o)
		{
			var stream = new MemoryStream();
			new BinaryFormatter().Serialize(stream, o);
			stream.Seek(0, SeekOrigin.Begin);

			return sha.ComputeHash(stream);
		}

		public static string GetSha(byte[] b) => string.Join("", sha.ComputeHash(b).Select(x => String.Format("{0:x2}", x)));
	}

	public class FileInfo
	{
		public readonly string Path;
		public readonly string Content;

		public FileInfo(string path, string content)
		{
			Path = path;
			Content = content;
		}
	}

	[Serializable]
	public class Id
	{
		public byte[] Bytes { get; private set; }

		public Id(byte[] b)
		{
			Bytes = b;
		}

		/// <summary>
		/// Equivalent to "git hash-object -w <file>"
		/// </summary>
		public static Id HashObject(object o) => new Id(Sha.Compute(o));

		public override string ToString() => Sha.GetSha(Bytes);

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(obj, null))
				return false;
			var otherbytes = ((Id) obj).Bytes;
			return Bytes.Length == otherbytes.Length && Bytes.Select((x, i) => new {x, i}).All(o => o.x == otherbytes[o.i]);
		}

		public static bool operator ==(Id a, Id b) => ReferenceEquals(a, b) || (!ReferenceEquals(a, null) && a.Equals(b));

		public static bool operator !=(Id a, Id b) => !(a==b);

		public override int GetHashCode() => Bytes.Aggregate(397 * Bytes.Length, (hash, aByte) => hash ^ (aByte * 397 * hash));
	}

	public class Storage
	{
		public Dictionary<Id, BlobNode> Blobs = new Dictionary<Id, BlobNode>();
		public Dictionary<Id, TreeNode> Trees = new Dictionary<Id, TreeNode>();
		public Dictionary<Id, CommitNode> Commits = new Dictionary<Id, CommitNode>();

		public Dictionary<string, Branch> Branches = new Dictionary<string, Branch>();
		public Head Head = new Head();
	}

	public class Branch
	{
		public Id Created { get; }
		public Id Tip { get; set; }

		public Branch(Id created, Id tip)
		{
			Created = created;
			Tip = tip;
		}
	}

	/// <summary>
	/// In git the file content of the file "HEAD" is either an ID or a reference to a branch.eg.
	/// "ref: refs/heads/master"
	/// </summary>
	public class Head
	{
		public Id Id { get; private set; }
		public string Branch { get; private set; }

		public void Update(string branch)
		{
			Branch = branch;
			Id = null;
		}

		public void Update(Id id)
		{
			Branch = null;
			Id = id;
		}

		public bool IsDetachedHead() => Id != null;

		public Id GetId(Storage s) => Id ?? s.Branches[Branch].Tip;
	}

	[Serializable]
	public class TreeNode
	{
		public ITreeLine[] Lines;

		public override string ToString() => string.Join("\n", Lines.Select(x => x.ToString()));
	}

	public interface ITreeLine
	{ }

	[Serializable]
	class BlobTreeLine : ITreeLine
	{
		public Id Id { get; private set; }
		public BlobNode Blob { get; private set; }
		public string Path { get; private set; }

		public BlobTreeLine(Id id, BlobNode blob, string path)
		{
			Id = id;
			Blob = blob;
			Path = path;
		}

		public override string ToString() => $"blob {Id} {Path}";
	}

	[Serializable]
	public class CommitNode
	{
		public DateTime Time;
		public TreeNode Tree;
		public string Author;
		public string Message;
		public Id[] Parents = new Id[0];
	}

	[Serializable]
	public class BlobNode
	{
		public string Content { get; }

		public BlobNode(string content)
		{
			Content = content;
		}
	}

	/// <summary>
	/// Mini clone of git
	/// Supports
	/// * commits
	/// * branches
	/// * detached heads
	/// * checkout old commits
	/// * logging
	/// </summary>
	public class KBGit
	{
		private readonly string repositoryName;
		public string CodeFolder { get; }
		const string Datafile = "kbgit.json";
		public Storage Hd;

		public KBGit(string repositoryName, string startpath)
		{
			this.repositoryName = repositoryName;
			CodeFolder = startpath;
			Path.Combine(CodeFolder, ".git", Datafile);
			LoadState();
		}

		public string FullName(string branchname) => branchname.Contains("/") ? branchname : repositoryName + "/" + branchname;

		public void LoadState()
		{
		}

		public void SaveState()
		{
		}

		/// <summary>
		/// Initialize a repo. eg. "git init"
		/// </summary>
		public void Init()
		{
			Hd = new Storage();
			var branch = FullName("master");
			CheckOut_b(branch, null);
			Hd.Head.Update(branch);
			SaveState();
			ResetCodeFolder();
		}

		/// <summary> update head to a branch</summary>
		public void Checkout(string branch)
		{
			var name = FullName(branch);
			if (!Hd.Branches.ContainsKey(name))
				throw new ArgumentOutOfRangeException($"No branch named \'{name}\'");

			Hd.Head.Update(Hd.Branches[name].Tip);
		}

		/// <summary> update head to an ID</summary>
		public void Checkout(Id position)
		{
			if (!Hd.Commits.ContainsKey(position))
				throw new ArgumentOutOfRangeException($"No commit id {position}");

			Hd.Head.Update(position);
		}

		/// <summary> Create a branch: e.g "git checkout -b foo" </summary>
		public void CheckOut_b(string name)
		{
			CheckOut_b(name, Hd.Head.GetId(Hd));
		}

		/// <summary> Create a branch: e.g "git checkout -b foo fb1234.."</summary>
		public void CheckOut_b(string name, Id position)
		{
			name = FullName(name);

			Hd.Branches.Add(name, new Branch(position, position));
			Hd.Head.Update(name);
		}

		/// <summary>
		/// Simulate syntax: e.g. "HEAD~2"
		/// </summary>
		public Id HeadRef(int numberOfPredecessors)
		{
			var result = Hd.Head.GetId(Hd);
			for (int i = 0; i < numberOfPredecessors; i++)
			{
				result = Hd.Commits[result].Parents.First();
			}

			return result;
		}

		public Id Commit(string message, string author, DateTime now, params FileInfo[] fileInfo)
		{
			var blobsInCommit = fileInfo.Select(x => new
			{
				file = x,
				blobid = new Id(Sha.Compute(x.Content)),
				blob = new BlobNode(x.Content)
			}).ToArray();

			var treeNode = new TreeNode()
			{
				Lines = blobsInCommit.Select(x => new BlobTreeLine(x.blobid, x.blob, x.file.Path)).ToArray(),
			};

			var parentCommitId = Hd.Head.GetId(Hd);
			var isFirstCommit = parentCommitId == null;
			var commit = new CommitNode
			{
				Time = now,
				Tree = treeNode,
				Author = author,
				Message = message,
				Parents = isFirstCommit ? new Id[0] : new[] {parentCommitId},
			};

			var treeNodeId = Id.HashObject(treeNode);
			if(!Hd.Trees.ContainsKey(treeNodeId))
				Hd.Trees.Add(treeNodeId, treeNode);

			foreach (var blob in blobsInCommit.Where(x => !Hd.Blobs.ContainsKey(x.blobid)))
			{
				Hd.Blobs.Add(blob.blobid, blob.blob);
			}

			var commitId = Id.HashObject(commit);
			Hd.Commits.Add(commitId, commit);

			if (Hd.Head.IsDetachedHead())
				Hd.Head.Update(commitId);
			else
				Hd.Branches[Hd.Head.Branch].Tip = commitId;

			SaveState();

			return commitId;
		}

		void ResetCodeFolder()
		{
			if (Directory.Exists(CodeFolder))
				Directory.Delete(CodeFolder, true);
			Directory.CreateDirectory(CodeFolder);
		}

		/// <summary>
		/// Delete a branch. eg. "git branch -D name"
		/// </summary>
		public void Branch_D(string branch)
		{
			var name = FullName(branch);
			Hd.Branches.Remove(name);
		}

		/// <summary>
		/// Change folder content to branch and move HEAD 
		/// </summary>
		public void CheckOut(string branch)
		{
			CheckOut(Hd.Branches[FullName(branch)].Tip);
		}

		/// <summary>
		/// Change folder content to commit id and move HEAD 
		/// </summary>
		public void CheckOut(Id id)
		{
			void UpdateHead()
			{
				var branch = Hd.Branches.FirstOrDefault(x => x.Value.Tip == id);
				if (branch.Key == null)
					Hd.Head.Update(id);
				else
					Hd.Head.Update(branch.Key);
			}

			ResetCodeFolder();

			UpdateHead();

			var commit = Hd.Commits[id];
			foreach (BlobTreeLine line in commit.Tree.Lines)
			{
				File.WriteAllText(Path.Combine(CodeFolder, line.Path), line.Blob.Content);
			}

			if (Hd.Head.IsDetachedHead())
			{
				Console.WriteLine(
					"You are in 'detached HEAD' state. You can look around, make experimental changes and commit them, and you can discard any commits you make in this state without impacting any branches by performing another checkout.");
			}
		}

		public void Log()
		{
			foreach (var branch in Hd.Branches)
			{
				Console.WriteLine($"Log for {branch.Key}");
				var nodes = GetReachableNodes(branch.Value.Tip);
				foreach (var comit in nodes.OrderByDescending(x => x.Value.Time))
				{
					var commitnode = comit.Value;
					var key = comit.Key.ToString().Substring(0, 7);
					var msg = commitnode.Message.Substring(0, Math.Min(40, commitnode.Message.Length));
					var author = $"{commitnode.Author}";

					Console.WriteLine($"* {key} - {msg} {commitnode.Time:yy - MM - dd hh: mm} <{author}> ");
				}
			}
		}

		/// <summary>
		/// Clean out unreferences nodes. Equivalent to "git gc"
		/// </summary>
		public void Gc()
		{
			var reachables = Hd.Branches.Select(x => x.Value.Tip)
				.Union(new[] {Hd.Head.GetId(Hd)})
				.SelectMany(x => GetReachableNodes(x))
				.Select(x => x.Key);

			var deletes = Hd.Commits.Select(x => x.Key)
				.Except(reachables);

			foreach (var delete in deletes)
			{
				Hd.Commits.Remove(delete);
			}
		}

		public FileInfo[] ScanFileSystem()
		{
			return new DirectoryInfo(CodeFolder).EnumerateFiles("*", SearchOption.AllDirectories)
				.Select(x => new FileInfo(x.FullName.Substring(CodeFolder.Length), File.ReadAllText(x.FullName)))
				.ToArray();
		}

		List<KeyValuePair<Id, CommitNode>> GetReachableNodes(Id id)
		{
			var result = new List<KeyValuePair<Id, CommitNode>>();
			GetReachableNodes(id, result);
			return result;
		}

		void GetReachableNodes(Id id, List<KeyValuePair<Id, CommitNode>> result)
		{
			var current = Hd.Commits[id];
			result.Add(new KeyValuePair<Id, CommitNode>(id, current));
			foreach (var parent in current.Parents)
			{
				GetReachableNodes(parent, result);
			}
		}
	}
}