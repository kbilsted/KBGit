using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace KbgSoft.KBGit
{

	/// <summary>
	/// Mini clone of git
	/// Supporting
	/// * commits
	/// * branches
	/// * detached heads
	/// * checkout old commits
	/// * logging
	/// </summary>
	public class KBGit
	{
		public const string KBGitFolderName = ".git";
		public string CodeFolder { get; }
		public Storage Hd;
		public RemotesHandling Remotes;

		public KBGit(string startpath)
		{
			CodeFolder = startpath;
			// Path.Combine(CodeFolder, KBGitFolderName, Datafile);
		}

		/// <summary>
		/// Initialize a repo. eg. "git init"
		/// </summary>
		public void InitializeRepository()
		{
			Hd = new Storage();
			CreateBranch("master", null);
			Remotes = new RemotesHandling(Hd.Remotes);
		}

		/// <summary> Create a branch: e.g "git checkout -b foo" </summary>
		public void CreateBranch(string name) => CreateBranch(name, Hd.Head.GetId(Hd));

		/// <summary> Create a branch: e.g "git checkout -b foo fb1234.."</summary>
		public void CreateBranch(string name, Id position)
		{
			Hd.Branches.Add(name, new Branch(position, position));
			Hd.ResetCodeFolder(CodeFolder, position);
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

			return commitId;
		}

		public Id Commit(string message, string author, DateTime now, params Fileinfo[] fileinfo)
		{
			var blobsInCommit = fileinfo.Select(x => new
			{
				file = x,
				blobid = new Id(ByteHelper.ComputeSha(x.Content)),
				blob = new BlobNode(x.Content)
			}).ToArray();

			var treeNode = new TreeNode(blobsInCommit.Select(x => new BlobTreeLine(x.blobid, x.blob, x.file.Path)).ToArray());
			var treeNodeId = Id.HashObject(treeNode);

			var parentCommitId = Hd.Head.GetId(Hd);
			var isFirstCommit = parentCommitId == null;
			var commit = new CommitNode
			{
				Time = now,
				Tree = treeNode,
				TreeId = treeNodeId,
				Author = author,
				Message = message,
				Parents = isFirstCommit ? new Id[0] : new[] { parentCommitId },
			};

			if (!Hd.Trees.ContainsKey(treeNodeId))
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

			return commitId;
		}

		internal void AddOrSetBranch(string branch, Branch branchInfo)
		{
			if (Hd.Branches.ContainsKey(branch))
				Hd.Branches[branch].Tip = branchInfo.Tip;
			else
				Hd.Branches.Add(branch, branchInfo);
		}

		/// <summary>
		/// Delete a branch. eg. "git branch -D name"
		/// </summary>
		public void DeleteBranch(string branch) => Hd.Branches.Remove(branch);

		/// <summary>
		/// Change HEAD to branch,e.g. "git checkout featurebranch"
		/// </summary>
		public void Checkout(string branch) => Checkout(Hd.Branches[branch].Tip);

		/// <summary>
		/// Change folder content to commit position and move HEAD 
		/// </summary>
		public void Checkout(Id id)
		{
			Hd.ResetCodeFolder(CodeFolder, id);
			Hd.Head.Update(id, Hd);
		}

		/// <summary>
		/// eg. "git log"
		/// </summary>
		public string Log()
		{
			var sb = new StringBuilder();
			foreach (var branch in Hd.Branches)
			{
				sb.AppendLine($"\r\nLog for {branch.Key}");

				if (branch.Value.Tip == null) // empty repo
					continue;

				var nodes = GetReachableNodes(branch.Value.Tip, branch.Value.Created);
				foreach (var comit in nodes.OrderByDescending(x => x.Value.Time))
				{
					var commitnode = comit.Value;
					var key = comit.Key.ToString().Substring(0, 7);
					var msg = commitnode.Message.Substring(0, Math.Min(40, commitnode.Message.Length));
					var author = $"{commitnode.Author}";

					sb.AppendLine($"* {key} - {msg} ({commitnode.Time:yyyy\\/MM\\/dd hh\\:mm\\:ss}) <{author}> ");
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
				.Union(new[] { Hd.Head.GetId(Hd) })
				.SelectMany(x => GetReachableNodes(x))
				.Select(x => x.Key);

			var deletes = Hd.Commits.Select(x => x.Key)
				.Except(reachables).ToList();

			deletes.ForEach(x => Hd.Commits.Remove(x));
		}

		internal void RawImportCommits(KeyValuePair<Id, CommitNode>[] commits, string branch, Branch branchInfo)
		{
			//Console.WriteLine("RawImportCommits");

			foreach (var commit in commits)
			{
				//Console.WriteLine("import c" + commit.Key);
				Hd.Commits.TryAdd(commit.Key, commit.Value);
				Hd.Trees.TryAdd(commit.Value.TreeId, commit.Value.Tree);

				foreach (var treeLine in commit.Value.Tree.Lines)
				{
					if (treeLine is BlobTreeLine b)
					{
						//Console.WriteLine("import b " + b.Id);
						Hd.Blobs.TryAdd(b.Id, b.Blob);
					}

					if (treeLine is TreeTreeLine t)
					{
						//Console.WriteLine("import t " + t.Id);
						Hd.Trees.TryAdd(t.Id, t.Tree);
					}
				}
			}

			AddOrSetBranch(branch, branchInfo);
		}

		public Fileinfo[] ScanFileSystem()
		{
			return new DirectoryInfo(CodeFolder).EnumerateFiles("*", SearchOption.AllDirectories)
				.Select(x => new Fileinfo(x.FullName.Substring(CodeFolder.Length), File.ReadAllText(x.FullName)))
				.ToArray();
		}

		public List<KeyValuePair<Id, CommitNode>> GetReachableNodes(Id from, Id downTo = null)
		{
			var result = new List<KeyValuePair<Id, CommitNode>>();
			GetReachableNodes(from);

			void GetReachableNodes(Id currentId)
			{
				var commit = Hd.Commits[currentId];
				result.Add(new KeyValuePair<Id, CommitNode>(currentId, commit));

				foreach (var parent in commit.Parents.Where(x => !x.Equals(downTo)))
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
				.Select(x => new { Content = File.ReadAllText(x.FullName), x.FullName })
				.Select(x => new BlobTreeLine(new Id(ByteHelper.ComputeSha(x.Content)), new BlobNode(x.Content), x.FullName.Substring(CodeFolder.Length))));

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
		public string ListBranches()
		{
			var branched = Hd.Branches
				.OrderBy(x => x.Key)
				.Select(x => $"{(Hd.Head.Branch == x.Key ? "*" : " ")} {x.Key}");

			var detached = Hd.Head.IsDetachedHead() ? $"* (HEAD detached at {Hd.Head.Id.ToString().Substring(0, 7)})\r\n" : "";

			return detached + string.Join("\r\n", branched);
		}
	}

	public class GrammarLine
	{
		public readonly string Explanation;
		public readonly string[] Grammar;
		public readonly Func<KBGit, string[], string> ActionOnMatch;

		public GrammarLine(string explanation, string[] grammar, Action<KBGit, string[]> actionOnMatch) : this(explanation, grammar, (git, arg) => { actionOnMatch(git, arg); return null; })
		{}

		public GrammarLine(string explanation, string[] grammar, Func<KBGit, string[], string> actionOnMatch)
		{
			Explanation = explanation; Grammar = grammar; ActionOnMatch = actionOnMatch;
		}
	}

	public class CommandlineHandling
	{
		public static readonly GrammarLine[] Config = 
		{
			new GrammarLine("Initialize an empty repo", new[] { "init"}, (git, args) => { git.InitializeRepository(); }),
			new GrammarLine("Make a commit", new[] { "commit", "-m", "<message>"}, (git, args) => { git.Commit(args[2], "author", DateTime.Now, git.ScanFileSystem()); }),
			new GrammarLine("Show the commit log", new[] { "log"}, (git, args) => git.Log()),
			new GrammarLine("Create a new new branch at HEAD", new[] { "checkout", "-b", "<branchname>"}, (git, args) => { git.CreateBranch(args[2]); }),
			new GrammarLine("Create a new new branch at commit id", new[] { "checkout", "-b", "<branchname>", "<id>"}, (git, args) => { git.CreateBranch(args[2], new Id(args[3])); }),
			new GrammarLine("Update HEAD", new[] { "checkout", "<id>"}, (git, args) => { git.Checkout(new Id(args[1])); }),
			new GrammarLine("Delete a branch", new[] { "branch", "-D", "<branchname>"}, (git, args) => { git.DeleteBranch(args[2]); }),
			new GrammarLine("List existing branches", new[] { "branch"}, (git, args) => { git.ListBranches(); }),
			new GrammarLine("Garbage collect", new[] { "gc" }, (git, args) => { git.Gc(); }),
			new GrammarLine("Start git as a server", new[] { "daemon", "<port>" }, (git, args) => { new GitServer(git).StartDaemon(int.Parse(args[1])); }),
			new GrammarLine("Pull code", new[] { "pull", "<remote-name>", "<branch>"}, (git, args) => { new GitNetworkClient().PullBranch(git.Hd.Remotes.First(x => x.Name == args[1]), args[2], git);}),
			new GrammarLine("Push code", new[] { "push", "<remote-name>", "<branch>"}, (git, args) => { new GitNetworkClient().PushBranch(git.Hd.Remotes.First(x => x.Name == args[1]), args[2], git.Hd.Branches[args[2]], null, git.GetReachableNodes(git.Hd.Branches[args[2]].Tip).ToArray()); }),
			new GrammarLine("List remotes", new[] { "remote", "-v"}, (git, args) => { git.Remotes.List(); }),
			new GrammarLine("Add remote", new[] { "remote", "add", "<remote-name>", "<url>"}, (git, args) => { git.Remotes.Add(new Remote(){Name = args[2], Url = new Uri(args[3])}); }),
			new GrammarLine("Remove remote", new[] { "remote", "rm", "<remote-name>"}, (git, args) => { git.Remotes.Remove(args[2]); }),
		};

		public string Handle(KBGit git, GrammarLine[] config, string[] cmdParams)
		{
			var match = config.SingleOrDefault(x => x.Grammar.Length == cmdParams.Length 
												 && x.Grammar.Zip(cmdParams, (gramar, arg) => gramar.StartsWith("<") || gramar == arg).All(m => m));

			return match == null 
				? $"KBGit Help\r\n----------\r\ngit {string.Join("\r\ngit ", config.Select(x => $"{string.Join(" ", x.Grammar),-34} - {x.Explanation}."))}" 
				: match.ActionOnMatch(git, cmdParams);
		}
	}

	public static class ByteHelper
	{
		static readonly SHA256 Sha = SHA256.Create();

		public static string ComputeSha(object o) => string.Join("", Sha.ComputeHash(Serialize(o)).Select(x => String.Format("{0:x2}", x)));

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
				return (T) new BinaryFormatter().Deserialize(ms);
			}
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
		public static Id HashObject(object o) => new Id(ByteHelper.ComputeSha(o));

		public override string ToString() => ShaId;
		public override bool Equals(object obj) => ShaId.Equals((obj as Id)?.ShaId);
		public override int GetHashCode() => ShaId.GetHashCode();
	}

	public class Storage
	{
		public Dictionary<Id, BlobNode> Blobs = new Dictionary<Id, BlobNode>();
		public Dictionary<Id, TreeNode> Trees = new Dictionary<Id, TreeNode>();
		public Dictionary<Id, CommitNode> Commits = new Dictionary<Id, CommitNode>();

		public Dictionary<string, Branch> Branches = new Dictionary<string, Branch>();
		public Head Head = new Head();
		public List<Remote> Remotes = new List<Remote>();

		internal void ResetCodeFolder(string codeFolder, Id position)
		{
			if (Directory.Exists(codeFolder))
				Directory.Delete(codeFolder, true);
			Directory.CreateDirectory(codeFolder);

			if (position != null)
			{
				var commit = Commits[position];
				foreach (BlobTreeLine line in commit.Tree.Lines)
				{
					File.WriteAllText(Path.Combine(codeFolder, line.Path), line.Blob.Content);
				}
			}
		}
	}

	[Serializable]
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

	[Serializable]
	public class Remote
	{
		public string Name;
		public Uri Url;
	}

	public class RemotesHandling
	{
		List<Remote> Remotes;

		public RemotesHandling(List<Remote> remotes)
		{
			Remotes = remotes;
		}

		/// <summary>
		/// List remotes Git remote -v
		/// </summary>
		public string List() => string.Join("\r\n", Remotes.Select(x => $"{x.Name,-12} {x.Url}"));

		/// <summary>
		/// Add a remote
		/// </summary>
		public void Add(Remote r) => Remotes.Add(r);

		/// <summary>
		/// Remove a remote
		/// </summary>
		public void Remove(string name) => Remotes = Remotes.Where(x => x.Name != name).ToList();
	}

	/// <summary>
	/// In git the file content of the file "HEAD" is either an ID or a reference to a branch.eg.
	/// "ref: refs/heads/master"
	/// </summary>
	[Serializable]
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
			var b = s.Branches.FirstOrDefault(x => x.Value.Tip.Equals(position));
			if(b.Key == null)
			{
				if (!s.Commits.ContainsKey(position))
					throw new ArgumentOutOfRangeException($"No commit with id '{position}'");

				Console.WriteLine("You are in 'detached HEAD' state. You can look around, make experimental changes and commit them, and you can discard any commits you make in this state without impacting any branches by performing another checkout.");

				Branch = null;
				Id = position;
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

		public BlobNode(string content) => Content = content;
	}

	[Serializable]
	public class GitPushBranchRequest
	{
		public KeyValuePair<Id, CommitNode>[] Commits { get; set; }
		public string Branch { get; set; }
		public Branch BranchInfo { get; set; }
		public Id LatestRemoteBranchPosition { get; set; }
	}

	[Serializable]
	public class GitPullResponse
	{
		public KeyValuePair<Id, CommitNode>[] Commits { get; set; }
		public Branch BranchInfo { get; set; }
	}

	/// <summary>
	/// Used for communicating with a git server
	/// </summary>
	public class GitNetworkClient
	{
		public void PushBranch(Remote remote, string branch, Branch branchInfo, Id fromPosition, KeyValuePair<Id, CommitNode>[] nodes)
		{
			var request = new GitPushBranchRequest() {Branch = branch, BranchInfo = branchInfo, LatestRemoteBranchPosition = fromPosition, Commits = nodes};
			var result = new HttpClient().PostAsync(remote.Url, new ByteArrayContent(ByteHelper.Serialize(request))).GetAwaiter().GetResult();
			Console.WriteLine($"Push status: {result.StatusCode}");
		}

		public void PullBranch(Remote remote, string branch, KBGit git)
		{
			var bytes = new HttpClient().GetByteArrayAsync(remote.Url + "?branch=" + branch).GetAwaiter().GetResult();
			var commits = ByteHelper.Deserialize<GitPullResponse>(bytes);
			git.RawImportCommits(commits.Commits, $"{remote.Name}/{branch}", commits.BranchInfo);
		}
	}

	public class GitServer 
	{
		private readonly KBGit git;
		private HttpListener listener;
		public bool? Running { get; private set; }

		public GitServer(KBGit git)
		{
			this.git = git;
		}

		public void Abort()
		{
			Running = false;
			listener?.Abort();
		}

		public void StartDaemon(int port)
		{
			Console.WriteLine($"Serving on http://localhost:{port}/");

			listener = new HttpListener();
			listener.Prefixes.Add($"http://localhost:{port}/");
			listener.Start();

			Running = true;
			while (Running.Value)
			{
				var context = listener.GetContext();
				try
				{
					if (context.Request.HttpMethod == "GET")
					{
						var branch = context.Request.QueryString.Get("branch");

						if (!git.Hd.Branches.ContainsKey(branch))
						{
							context.Response.StatusCode = 404;
							context.Response.Close();
							continue;
						}

						context.Response.Close(ByteHelper.Serialize(new GitPullResponse()
						{
							BranchInfo = git.Hd.Branches[branch],
							Commits = git.GetReachableNodes(git.Hd.Branches[branch].Tip).ToArray()
						}), true);
					}

					if(context.Request.HttpMethod == "POST")
					{
						var req = ByteHelper.Deserialize<GitPushBranchRequest>(context.Request.InputStream);
						// todo check if we are loosing commits when updating the branch pointer..we get a fromid with the request
						git.RawImportCommits(req.Commits, req.Branch, req.BranchInfo);
						context.Response.Close();
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"\n\n{DateTime.Now}\n{e} - {e.Message}");
					context.Response.StatusCode = 500;
					context.Response.Close();
				}
			}
		}
	}
}