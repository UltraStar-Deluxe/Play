using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Flurl;
using Flurl.Util;
using HtmlAgilityPack;
using Opportunity.LrcParser;
using UnityEngine;

public static class LrcUtils
{
    
    /**
     * Demonstrates usage of the LrcParser library.
     */
    public static void LrcParserDemo()
    {
        string lrcText = @"
                [ar:Some Artist]
                [00:00.88]Freude, schöner Götterfunken, Tochter aus Elysium
                [00:10.16]Wir betreten feuertrunken, Himmlische, dein Heiligtum.
                [00:19.58]Deine Zauber binden wieder, was die Mode streng geteilt,
                [00:29.19]alle Menschen werden Brüder, wo dein sanfter Flügel weilt.
        ";
        IParseResult<Line> parseResult = Lyrics.Parse(lrcText);
        if (!parseResult.Exceptions.IsNullOrEmpty())
        {
            parseResult.Exceptions.ForEach(e => Debug.LogException(e));
        }
        else
        {
            Debug.Log($"LRC artist: {parseResult.Lyrics.MetaData.Artist}");
            parseResult.Lyrics.Lines.ForEach(line => Debug.Log(line.Timestamp + " | " + line.Content));
        }
    }

    /**
     * Demonstrates download of lyrics from lyricsify.com could be achieved.
     */
    public static void SearchLyricsOnLyricsify(string searchTerm, MonoBehaviour monoBehaviour)
    {
        // TODO: Use Flurl and async for the requests.
        Regex hrefRegex = new Regex("href=\"(\\/lrc\\/.+?)\"");
        string url = "https://www.lyricsify.com"
            .AppendPathSegment("search")
            .SetQueryParam("q", searchTerm);
        monoBehaviour.StartCoroutine(WebRequestUtils.LoadTextFromUri(url, searchResponse =>
        {
            MatchCollection hrefMatchCollection = hrefRegex.Matches(searchResponse);
            string bestTargetUrl = "";
            foreach (Match hrefMatch in hrefMatchCollection)
            {
                string targetUrl = hrefMatch.Groups[1].Value;
                Debug.Log("targetUrl: " + targetUrl);

                if (bestTargetUrl.IsNullOrEmpty())
                {
                    bestTargetUrl = targetUrl;
                }
            }

            if (!bestTargetUrl.IsNullOrEmpty())
            {
                bestTargetUrl = $"https://www.lyricsify.com"
                    .AppendPathSegment(bestTargetUrl);
                Debug.Log($"Fetching lyrics from {bestTargetUrl}");
                monoBehaviour.StartCoroutine(WebRequestUtils.LoadTextFromUri(bestTargetUrl, lyricsResponse =>
                {
                    File.WriteAllText($"{Application.temporaryCachePath}/lyricsResponse.txt", lyricsResponse);
                    Debug.Log($"saved lyrics response to {Application.temporaryCachePath}/lyricsResponse.txt");

                    ParseLyricsifyLyricsResponse(lyricsResponse);
                }));
            }
        }));
    }

    private static void ParseLyricsifyLyricsResponse(string lyricsResponse)
    {
        HtmlDocument html = new HtmlDocument();
        html.LoadHtml(lyricsResponse);

        Regex lrcLineRegex = new Regex(@"\[\w{2}\:.+\][^\<]*");
        List<HtmlNode> lyricsDivNodes = html.DocumentNode.Descendants("div").Where(div =>
                div.Id.StartsWith("lyrics_")
                && div.Id.EndsWith("_details"))
            .ToList();
        List<StringBuilder> stringBuilders = new();
        foreach (HtmlNode node in lyricsDivNodes)
        {
            StringBuilder sb = new();
            stringBuilders.Add(sb);

            MatchCollection lrcLineMatchCollection = lrcLineRegex.Matches(node.InnerText);
            foreach (Match lrcLineMatch in lrcLineMatchCollection)
            {
                sb.Append(lrcLineMatch);
            }
        }

        StringBuilder longestStringBuilder = stringBuilders.FindMaxElement(it => it.Length);

        string lrcString = longestStringBuilder.ToString();
        Debug.Log($"LRC Text: {lrcString}");

        // Parse lrc text
        IParseResult<Line> parseResult = Lyrics.Parse(lrcString);
        if (!parseResult.Exceptions.IsNullOrEmpty())
        {
            parseResult.Exceptions.ForEach(e => Debug.LogException(e));
        }
        else
        {
            Debug.Log($"LRC meta data: {parseResult.Lyrics.MetaData.ToKeyValuePairs().ToCsv()}");
            Debug.Log("LRC Lines:");
            parseResult.Lyrics.Lines.ForEach(line => Debug.Log(line.Timestamp + " | " + line.Content));
        }
    }
}
