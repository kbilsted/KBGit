# KBGit - Git implemented in 500 lines of code (or less ...)

Project statistics:  <!--start-->
[![Stats](https://img.shields.io/badge/Code_lines-224-ff69b4.svg)]()
[![Stats](https://img.shields.io/badge/Doc_lines-19-ff69b4.svg)]()
<!--end-->

I've been an avid user of Git and Hg for over 10 years - but I've never dug into their implementation details. 
I did not long ago, and I found beaty! Such beauty that I wanted to share the great ideas of turning something as conceptually complex as a source code versioning system 
into a simple implementation. 

I want to code as much of Git's functionality in 500 lines of code or less! I chose to line limit this project for two reasons. 
First, anything 500 lines of code should be explainable to any real programmer. Secondly, inspiration from Terry A. Davis' work on [TempleOS](http://www.templeos.org)


>	Everybody is obsessed, Jedi mind-tricked, by the notion that when you scale-up, 
>	it doesn't get bad, it gets worse.  They automatically think things are going to 
>	get bigger.  Guess what happens when you scale down?  It doesn't get good, it 
>	gets better!
>	-- Terry A. Davis (https://templeos.sheikhs.space/Wb/Doc/Strategy.html)

I wanted things to be fair. Now 500 lines of code is not a lot, and I could easily see myself spiraling into an code-obfuscation contest with myself in order to save lines of code. To prevent any code styles from getting in my way, I'm counting line using a   [simple line counting project](https://github.com/kbilsted/LineCounter.Net) which roughly counts all but blank lines (empty lines, lines only containing `{`, `}`,...). At the top of the readme you can track my progress.


## Features implemented

 * commits
 * branches
 * detached heads
 * checkout old commits
 * logging (90%)


## planned work 
	
 * push
 * pull
 * sub-folder support
 * graphical logging
 * git INDEX rather than scanning files
 * blob compression


I will blog about the implementation on [http://firstclassthoughts.co.uk/](http://firstclassthoughts.co.uk/)