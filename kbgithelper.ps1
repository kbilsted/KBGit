
function CreateGitWithCommits()
{
    $git = "C:\src\KBGit\bin\Debug\netcoreapp2.0\win10-x64\KBGit.exe"
	"init"
	iex "$git init"

	"adding a"
	& echo "aaaa" > a.txt
	iex "$git commit -m 'file a.txt'"

	"adding b"
	& echo "bbbb" > b.txt
	iex "$git commit -m 'b file'"

	iex "$git log"
	iex "$git daemon 8080"
}

function CloneFrom8080()
{
    $git = "C:\src\KBGit\bin\Debug\netcoreapp2.0\win10-x64\KBGit.exe"
	iex "$git clone http://localhost 8080 master"
}