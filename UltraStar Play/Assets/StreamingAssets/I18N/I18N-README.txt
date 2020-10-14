Encoding
--------
These properties files must be encoded in UTF-8 with BOM.
Note that this encoding differs from the encoding that properties files in Java typically use.

Conventions
-----------
- Use camelCase 
- Separate hierarchy levels (e.g. "Scene > Button > Label" vs. "Scene > Button > Tooltip") by underscore.
- Example: `sceneOne_startButton_toolTip = Start the game`

Syntax
------
- Key and value are separated by an equals sign. Whitespace around the key and value is trimmed.
- Comments start with #
- You can quote strings if needed (e.g. when you want whitespace at the end of the value).
    - Example: `quotesExample = "These quotes are optional"
- Use curly braces for named placeholders.
    - Example: `helloLabel = Hello {name}`
- Use \n for line breaks.
    - Example: `multiLineExample = This is the first line\nThis is the second line`

Examples:
---------
# This is a comment
demoScene_button_quit_label = Quit
demoScene_button_quit_tooltip = Closes the game
demoScene_button_start = "Start"
demoScene_hello = Hello {name}!
demoScene_multiline = First line\nSecond line