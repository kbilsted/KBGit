# KBGit - Git implemented from scratch in 500 lines of code (or less ...)

Project statistics:  <!--start-->
[![Stats](https://img.shields.io/badge/Code_lines-245-ff69b4.svg)]()
[![Stats](https://img.shields.io/badge/Doc_lines-20-ff69b4.svg)]()
<!--end-->

I've been an avid user of Git and Hg for over 10 years - but only recently did I dig into their implementation details. And 
I found beauty! A very simple model for something as complex as a source code versioning system. So simple it can be implemented in 500 lines of code! 

I've line-limited this project for two reasons: Anything in 500 lines is explainable. Secondly, inspirations from Terry A. Davis' work on [TempleOS](http://www.templeos.org)

>	Everybody is obsessed, Jedi mind-tricked, by the notion that when you scale-up, 
>	it doesn't get bad, it gets worse.  They automatically think things are going to 
>	get bigger.  Guess what happens when you scale down?  It doesn't get good, it 
>	gets better!
>	-- Terry A. Davis (https://templeos.sheikhs.space/Wb/Doc/Strategy.html)

I want a fair game, and 500 lines of code is not a lot. To prevent myself from spiraling into an code-obfuscation contest in order to save a few lines of code, I'm counting lines using a [simple line counting library](https://github.com/kbilsted/LineCounter.Net) 
which count only semantic lines (i.e. exclude empty lines, lines only containing `{`, `}`,...). At the top of the readme you can track my progress...


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
 * store git state on disk 


I will blog about the implementation on [http://firstclassthoughts.co.uk/](http://firstclassthoughts.co.uk/)