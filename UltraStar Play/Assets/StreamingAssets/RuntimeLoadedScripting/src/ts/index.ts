class MyHighscoreProvider implements HighscoreProvider
{
    score: number;
    name: string = "My Highscore Provider";

    constructor()
    {
        this.score = 2000;
    }

    getScore(): number
    {
        return this.score
    }

    getNoteCount(songMeta: SongMeta): number
    {
        debugger;
        return 0;
    }
}

var highscoreProvider = new MyHighscoreProvider();
runtimeScriptRegistry.addHighscoreProvider(highscoreProvider)

// function sayHello()
// {
//     log("Hello world!");

//     var x = 1;
//     log(`Value of x: ${x}`);
//     x = x + 1;
//     log(`Value of x: ${x}`);
//     x = x + 1;
//     log(`Value of x: ${x}`);
// }

// sayHello();