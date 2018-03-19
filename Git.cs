using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace KbgSoft.KBGit
{
	public static class Sha
	{
		static readonly SHA256 sha = SHA256.Create();

		public static string Compute(object o)
		{
			var stream = new MemoryStream();
			new BinaryFormatter().Serialize(stream, o);
			stream.Seek(0, SeekOrigin.Begin);

			return string.Join("", sha.ComputeHash(stream).Select(x => String.Format("{0:x2}", x)));
		}
	}

	public class Fileinfo
	{
		public readonly string Path;
		public readonly string Content;

		public Fileinfo(string path, string content)
		{
			Path = path;
			Content = content;
		}
	}

	[Serializable]
	public class Id
	{
		public string ShaId { get; private set; }

		public Id(string sha)
		{
			if(sha == null || sha.Length != 64)
				throw new ArgumentException("Not a valid SHA");
			ShaId = sha;
		}

		/// <summary>
		/// Equivalent to "git hash-object -w <file>"
		/// </summary>
		public static Id HashObject(object o) => new Id(Sha.Compute(o));

		public override string ToString() => ShaId;

		public override bool Equals(object obj) => ShaId.Equals((obj as Id)?.ShaId);

		public static bool operator ==(Id a, Id b) => ReferenceEquals(a, b) || (!ReferenceEquals(a, null) && a.Equals(b));

		public static bool operator !=(Id a, Id b) => !(a==b);

		public override int GetHashCode() => ShaId.GetHashCode();
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

		public void Update(string branch, Storage s)
		{
			if (!s.Branches.ContainsKey(branch))
				throw new ArgumentOutOfRangeException($"No branch named \'{branch}\'");

			Branch = branch;
			Id = null;
		}

		public void Update(Id position, Storage s)
		{
			var b = s.Branches.FirstOrDefault(x => x.Value.Tip == position);
			if(b.Key==null)
			{
				if (!s.Commits.ContainsKey(position))
					throw new ArgumentOutOfRangeException($"No commit with id '{position}'");
				Branch = null;
				Id = position;

				Console.WriteLine(
					"You are in 'detached HEAD' state. You can look around, make experimental changes and commit them, and you can discard any commits you make in this state without impacting any branches by performing another checkout.");
			}
			else
			{
				Update(b.Key, s);
			}
		}

		public bool IsDetachedHead() => Id != null;

		public Id GetId(Storage s) => Id ?? s.Branches[Branch].Tip;
	}

	[Serializable]
	public class TreeNode
	{
		public ITreeLine[] Lines;
		public TreeNode(ITreeLine[] lines)
		{
			Lines = lines;
		}

		public override string ToString() => string.Join("\n", Lines.Select(x => x.ToString()));
	}

	public interface ITreeLine
	{
		void Visit(Action<ITreeLine> code);
	}

	[Serializable]
	public class BlobTreeLine : ITreeLine
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

		public override string ToString() => $"blob {Path}";

		public void Visit(Action<ITreeLine> code) => code(this);
	}

	[Serializable]
	public class TreeTreeLine : ITreeLine
	{
		public Id Id { get; private set; }
		public TreeNode Tree { get; private set; }
		public string Path { get; private set; }

		public TreeTreeLine(Id id, TreeNode tree, string path)
		{
			Id = id;
			Tree = tree;
			Path = path;
		}

		public override string ToString() => $"tree {Tree.Lines.Length} {Path}\r\n{string.Join("\r\n", Tree.Lines.Select(x => x.ToString()))}";

		public void Visit(Action<ITreeLine> code)
		{
			code(this);

			foreach (var line in Tree.Lines)
				line.Visit(code);
		}
	}

	[Serializable]
	public class CommitNode
	{
		public DateTime Time;
		public TreeNode Tree;
		public Id TreeId;
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
		public const string KBGitFolderName = ".git";
		private readonly string repositoryName;
		public string CodeFolder { get; }
		const string Datafile = "kbgit.json";
		public Storage Hd;

		public KBGit(string repositoryName, string startpath)
		{
			this.repositoryName = repositoryName;
			LoadState();
			CodeFolder = startpath;
			// Path.Combine(CodeFolder, KBGitFolderName, Datafile);
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
			SaveState();
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
			ResetCodeFolder(position);
			Hd.Head.Update(name, Hd);
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

		/// <summary>
		/// Equivalent to "git hash-object -w <file>"
		/// </summary>
		public Id HashObject(string content) => Id.HashObject(content);

		public Id Commit(string message, string author, DateTime now)
		{
			var composite = FileSystemScanFolder(CodeFolder);
			composite.Visit(x =>
			{
				if (x is TreeTreeLine t)
					Hd.Trees.TryAdd(t.Id, t.Tree);
				if (x is BlobTreeLine b)
					Hd.Blobs.TryAdd(b.Id, b.Blob);
			});

			var parentCommitId = Hd.Head.GetId(Hd);
			var isFirstCommit = parentCommitId == null;
			var commit = new CommitNode
			{
				Time = now,
				Tree = composite.Tree,
				TreeId = composite.Id,
				Author = author,
				Message = message,
				Parents = isFirstCommit ? new Id[0] : new[] { parentCommitId },
			};

			var commitId = Id.HashObject(commit);
			Hd.Commits.Add(commitId, commit);

			if (Hd.Head.IsDetachedHead())
				Hd.Head.Update(commitId, Hd);
			else
				Hd.Branches[Hd.Head.Branch].Tip = commitId;

			SaveState();

			return commitId;
		}

		public Id Commit(string message, string author, DateTime now, params Fileinfo[] fileinfo)
		{
			var blobsInCommit = fileinfo.Select(x => new
			{
				file = x,
				blobid = new Id(Sha.Compute(x.Content)),
				blob = new BlobNode(x.Content)
			}).ToArray();

			var treeNode = new TreeNode(blobsInCommit.Select(x => new BlobTreeLine(x.blobid, x.blob, x.file.Path)).ToArray());

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
				Hd.Head.Update(commitId, Hd);
			else
				Hd.Branches[Hd.Head.Branch].Tip = commitId;

			SaveState();

			return commitId;
		}

		void ResetCodeFolder(Id position)
		{
			if (Directory.Exists(CodeFolder))
				Directory.Delete(CodeFolder, true);
			Directory.CreateDirectory(CodeFolder);

			if (position != null)
			{
				var commit = Hd.Commits[position];
				foreach (BlobTreeLine line in commit.Tree.Lines)
				{
					File.WriteAllText(Path.Combine(CodeFolder, line.Path), line.Blob.Content);
				}
			}
		}

		/// <summary>
		/// Delete a branch. eg. "git branch -D name"
		/// </summary>
		public void Branch_D(string branch)
		{
			var name = FullName(branch);
			Hd.Branches.Remove(name);
		}

		public void Checkout(string branch)
		{
			if (!Hd.Branches.ContainsKey(branch))
				branch = FullName(branch);
			Checkout(Hd.Branches[branch].Tip);
		}

		/// <summary>
		/// Change folder content to commit position and move HEAD 
		/// </summary>
		public void Checkout(Id id)
		{
			ResetCodeFolder(id);
			Hd.Head.Update(id, Hd);
		}

		public string Log()
		{
			var sb = new StringBuilder();
			foreach (var branch in Hd.Branches)
			{
				sb.AppendLine($"Log for {branch.Key}");
				var nodes = GetReachableNodes(branch.Value.Tip);
				foreach (var comit in nodes.OrderByDescending(x => x.Value.Time))
				{
					var commitnode = comit.Value;
					var key = comit.Key.ToString().Substring(0, 7);
					var msg = commitnode.Message.Substring(0, Math.Min(40, commitnode.Message.Length));
					var author = $"{commitnode.Author}";

					sb.AppendLine($"* {key} - {msg} ({commitnode.Time:yyyy/MM/dd hh\\:mm\\:ss}) <{author}> ");
				}
			}

			return sb.ToString();
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

		public Fileinfo[] ScanFileSystem()
		{
			return new DirectoryInfo(CodeFolder).EnumerateFiles("*", SearchOption.AllDirectories)
				.Select(x => new Fileinfo(x.FullName.Substring(CodeFolder.Length), File.ReadAllText(x.FullName)))
				.ToArray();
		}

		List<KeyValuePair<Id, CommitNode>> GetReachableNodes(Id id)
		{
			var result = new List<KeyValuePair<Id, CommitNode>>();
			GetReachableNodes(id);

			void GetReachableNodes(Id currentId)
			{
				var commit = Hd.Commits[currentId];
				result.Add(new KeyValuePair<Id, CommitNode>(currentId, commit));
				foreach (var parent in commit.Parents)
				{
					GetReachableNodes(parent);
				}
			}

			return result;
		}

		public TreeTreeLine FileSystemScanFolder(string path) => MakeTreeTreeLine(path);

		public ITreeLine[] FileSystemScanSubFolder(string path)
		{
			var entries = new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).ToArray();

			var tree = new List<ITreeLine>();

			tree.AddRange(entries.OfType<FileInfo>()
				.Select(x => new {Content = File.ReadAllText(x.FullName), x.FullName})
				.Select(x => new BlobTreeLine(new Id(Sha.Compute(x.Content)), new BlobNode(x.Content), x.FullName.Substring(CodeFolder.Length))));

			tree.AddRange(entries.OfType<DirectoryInfo>()
				.Where(x => !x.FullName.EndsWith(KBGitFolderName))
				.Select(x => MakeTreeTreeLine(x.FullName)));

			return tree.ToArray();
		}

		private TreeTreeLine MakeTreeTreeLine(string path)
		{
			var folderentries = FileSystemScanSubFolder(path);
			var treenode = new TreeNode(folderentries);
			var id = Id.HashObject(folderentries);

			return new TreeTreeLine(id, treenode, path.Substring(CodeFolder.Length));
		}

		/// <summary>
		/// return all branches and highlight current branch: "git branch"
		/// </summary>
		public string Branch()
		{
			var branched = Hd.Branches
				.OrderBy(x => x.Key)
				.Select(x => $"{(Hd.Head.Branch == x.Key ? "*" : " ")} {x.Key}");
			var headInfo = Hd.Head.IsDetachedHead()
				? new[] { $"* (HEAD detached at {Hd.Head.Id.ToString().Substring(0, 7)})" }
				: new string[0];

			return string.Join("\r\n", headInfo.Concat(branched));
		}
	}
}