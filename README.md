# KBGit - Git implemented from scratch in 500 lines of code (or less ...)

Project statistics:  <!--start-->
[![Stats](https://img.shields.io/badge/Code_lines-338-ff69b4.svg)]()
[![Stats](https://img.shields.io/badge/Doc_lines-22-ff69b4.svg)]()
<!--end-->

Recently I dug into their implementation details of Git. The simplicity of the implementation was completely mind-boggling. 
What a gem I found! Such a simple model for something conceptually complex as a source code versioning system. I almost couldn't believe it!

I wanted to share the beauty of the details of Git's inner workings. So how to best do this? 
I found inspiration from Terry A. Davis' work on [TempleOS](http://www.templeos.org)

>	Everybody is obsessed, Jedi mind-tricked, by the notion that when you scale-up, 
>	it doesn't get bad, it gets worse.  They automatically think things are going to 
>	get bigger.  Guess what happens when you scale down?  It doesn't get good, it 
>	gets better!
>	-- Terry A. Davis (https://templeos.sheikhs.space/Wb/Doc/Strategy.html)

So why would you want to grok this code? The reward reaped is that you'll find many of the operations of Git much more natural. They will suddenly make sense.
In order to have a fighting chance of conveying anything to any reader (and as Terry says). Less is more. **I challenged myself to keep the implementation 
to a maximum of 500 lines of code.. for a "complete re-implementation of git"!**

I want a fair game, though. And 500 lines of code is not a lot. To prevent myself from spiraling into an code-obfuscation contest in order to save 
a few lines of code, I'm counting lines using a [simple line counting library](https://github.com/kbilsted/LineCounter.Net) 
which count only semantic lines (i.e. exclude empty lines, lines only containing `{`, `}`,...). 

KBGit..what?? My initials are *K.B.G.* - hence the name KBGit :-)

## Features implemented

 * commits
 * branches
   * create
   * delete
   * list
   * detached heads
   * HEAD branch
 * checkout branches or commits
 * logging (90% done missing start-end of branching)
 * push + pull
 * remotes


## Planned work 
	
 * sub-folder support (90% done)
 * logging (graphical)
 * git INDEX rather than scanning files
 * blob compression
 * store git state on disk 
 * command line parser


I will blog about the implementation on [http://firstclassthoughts.co.uk/](http://firstclassthoughts.co.uk/) 
when the implementation has stabilized. Comments etc. are much welcommed.