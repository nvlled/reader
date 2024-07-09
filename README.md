# reader

This is program to assist or improve reading
experience by reducing eye movement. This
is done by showing chunks of words one
at a time, with large text size.

https://github.com/nvlled/reader/assets/916556/05ad322d-29fc-4c7b-9b87-3e639c6534e3

This is very much a work-in-progress project.
Currently, the program works by reading the
the text copied (with ctrl-c or right-click + copy).
In the future, I'm planning on making
it a proper PDF and epub reader. But for now,
copy/pasting works for my use case.

I'm still testing it to find
the optimal reading experience. So far,
I've managed to read about 200 pages with it. I'm
pretty comfortable with the current config,
although it might be too particular for
my preference and reading style.

The primary goal is reading comfort
for long or large blocks of text. Secondary
(and optional) goal is to improve reading speed.

# Download and setup

1. Install godot4.2 (dotnet version)
2. Download project
3. Open project with godot
4. Run with _F5_

TODO: upload binary blobs

# Usage

1. Open the program
2. Select and copy a text to read
3. The program will automatically show the copied text

# Keyboard controls

- _space_ or _enter_ or _right_ - move forward to next part
- _backsppace_ or _left_ - move backward to previous part
- _pageup_ - move ten steps backward
- _pagedown_ - move ten steps forward
- _home_ - move at the start
- _end_ - move at the end
