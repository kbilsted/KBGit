namespace KbgSoft.KBGit
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var git = new KBGit(".");
			new CommandlineHandling().Handle(git, CommandlineHandling.Config, args);
		}
	}
}
