Encoding
--------
These properties files must be encoded in UTF-8 with BOM.
Note that this encoding differs from the encoding that properties files in Java typically use.

Conventions
-----------
- Make keys all lower_case with underscore to separate words.
- Separate hierarchy levels (e.g. Scene > Button > Label, Scene > Button > Tooltip) by dots.

Syntax
------
- Key and value are separated by an equals sign. Whitespace around the key and value is trimmed.
- Comments start with #
- You can quote strings if needed (e.g. when you want whitespace at the end of the value).
- Use curly braces for named placeholders.
- Use \n for line breaks.

Examples:
---------
# This is a comment
demo_scene.button.quit.label = Quit
demo_scene.button.quit.tooltip = Closes the game
demo_scene.button.start = "Start"
demo_scene.hello = Hello {name}!
demo_scene.multiline = First line\nSecond line