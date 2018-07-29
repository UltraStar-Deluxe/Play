## Contributing
First off, thank you for considering contributing to UltraStar Play. It's people like you that make UltraStar Play such a great karaoke game.
Following these guidelines helps to communicate that you respect the time of the developers managing and developing this open source project. In return, they should reciprocate that respect in addressing your issue, assessing changes, and helping you finalize your pull requests.

We are a free/libre open source project and we love to receive contributions from our community — you! There are many ways to contribute, from writing tutorials or blog posts, improving the documentation, submitting bug reports and feature requests or writing code which can be incorporated into the game itself.
Please, don't use the issue tracker for support questions. Check whether the [UltraStar Play chat on Gitter](https://gitter.im/UltraStar-Deluxe/Play) can help with your issue. If your problem is not strictly UltraStar Play specific, there are also a couple of forums out there regarding karaoke games.

## Open Development
All work on UltraStar Play happens directly on GitHub. Both core team members and external contributors send pull requests which go through the same review process.

## Branch Organization
We will do our best to keep the master branch in good shape, with tests passing at all times. But in order to move fast, we will make changes that your custom changes might not be compatible with. We recommend that you use the latest stable version of the game. Please use feature branches when working on more complex changes, so others can follow and contribute, too.

If you send a pull request, please do it against the master branch.

## How to suggest a feature or enhancement
This project just started! Please wait for the first few versions to be released, before requesting exciting new stuff! If you just want to play some sing-along karaoke, we suggest you to use the much more stable (but old and dusty) [UltraStar Deluxe](https://github.com/UltraStar-Deluxe/USDX/releases) game.

## Responsibilities

- Ensure cross-platform compatibility for every change that's accepted. Windows, Linux, Android, Xbox, iOS.
- Ensure that code that goes into master branch meets all requirements from the requirements list below
- Create issues for any major changes and enhancements that you wish to make. Discuss things transparently and get community feedback.
- Don't add any classes to the codebase unless needed. Err on the side of using functions.
- Keep feature versions as small as possible, preferably one new major feature per version.
- Be welcoming to newcomers and encourage diverse new contributors from all backgrounds. See the [Contributor Covenant](https://www.contributor-covenant.org/) Community Code of Conduct.
- Take the time to get things right. Pull Requests (PR) almost always require additional improvements to meet the bar for quality. Be very strict about quality. This usually takes several commits on top of the original PR.
- Update documentation where necessary, write documentation when required. Use this repositories' wiki when targeting users / players of this game. Write green code or text files when targeting other developers.

## Requirements for code contributions to master branch
- try to only contribute working code, no dead code, no "soon to be used" code and no "will fix it soon" code
- for C# code, follow the usual C# / .Net coding style rules from Microsoft and Unity
- only strictly typed variables
- camelCase: parameter, 
- PascalCase: Method, Class, EEnum, NameSpace, Property, IInterface
- prefixes: E for enums, I for interfaces, m_ for members, S for scenes
- no acronyms, except for the above mentioned prefixes
- only use public where necessary, use static/readonly where possible, avoid protected
- no huge methods, try to reduce complexity, write readable code -> see [Clean Code Cheat Sheet](https://www.bbv.ch/images/bbv/pdf/downloads/V2_Clean_Code_V3.pdf)
- when copying others peoples/projects code, check licenses
