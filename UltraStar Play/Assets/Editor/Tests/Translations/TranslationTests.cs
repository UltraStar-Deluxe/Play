using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEngine;

public class TranslationTests
{
    private List<MissingTranslation> ignoredMissingTranslations;

    [Test]
    public void UxmlFilesAreTranslated()
    {
        List<string> uxmlFilesInAssets = GetFilesInFolderRecursive("Assets", "*.uxml")
            .ToList();
        List<string> uxmlFilesInPlayShared = GetFilesInFolderRecursive("Packages/playsharedui/Runtime", "*.uxml")
            .ToList();
        List<string> uxmlFiles = uxmlFilesInPlayShared
            .Union(uxmlFilesInAssets)
            .Where(uxmlFile => !IsIgnoredFile(uxmlFile))
            .ToList();

        List<MissingTranslation> allMissingTranslations = uxmlFiles
            .SelectMany(uxmlFile => GetMissingTranslationsInUxmlFile(uxmlFile))
            .Where(missingTranslation => !IsIgnoredMissingTranslation(missingTranslation))
            .ToList();
        Dictionary<string, List<MissingTranslation>> uxmlFileToMissingTranslations = allMissingTranslations
            .GroupBy(it => it.File)
            .ToDictionary(it => it.Key, it => it.ToList());

        uxmlFileToMissingTranslations.Keys.ForEach(uxmlFile =>
        {
            List<MissingTranslation> missingTranslations = uxmlFileToMissingTranslations[uxmlFile];
            Debug.LogError($"Missing translations in file: {Path.GetFileName(uxmlFile)}\n    "
                           + missingTranslations.Select(it => it.ToUxmlString())
                               .JoinWith("\n    "));
        });

        Assert.IsEmpty(allMissingTranslations, "There are missing translations in UXML files");
    }

    private bool IsIgnoredFile(string file)
    {
        string fileName = Path.GetFileName(file);
        List<string> ignoredFileNames = new List<string>()
        {
            "CFXR_WelcomeScreen.uxml",
        };
        if (ignoredFileNames.Contains(fileName))
        {
            return true;
        }

        return false;
    }

    private bool IsIgnoredMissingTranslation(MissingTranslation missingTranslation)
    {
        if (ignoredMissingTranslations.IsNullOrEmpty())
        {
            LoadIgnoredMissingTranslations();
        }

        string missingTranslationFileName = Path.GetFileName(missingTranslation.File);
        return ignoredMissingTranslations.AnyMatch(ignoredMissingTranslation =>
        {
            string ignoredMissingTranslationFileName = Path.GetFileName(ignoredMissingTranslation.File);
            return (ignoredMissingTranslationFileName == "*"
                    || string.Equals(ignoredMissingTranslationFileName, missingTranslationFileName, StringComparison.InvariantCultureIgnoreCase))
                   && (ignoredMissingTranslation.ElementLocalName == "*"
                       || string.Equals(ignoredMissingTranslation.ElementLocalName, missingTranslation.ElementLocalName, StringComparison.InvariantCultureIgnoreCase))
                   && (ignoredMissingTranslation.NameAttributeValue == "*"
                       || string.Equals(ignoredMissingTranslation.NameAttributeValue, missingTranslation.NameAttributeValue, StringComparison.InvariantCultureIgnoreCase));
        });
    }

    private void LoadIgnoredMissingTranslations()
    {
        ignoredMissingTranslations = new();

        string[] lines = File.ReadAllLines("Assets/Editor/Tests/Translations/IgnoredMissingTranslations.csv");
        // Start at index 1 to skip header line
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(",");
            string fileName = values[0].Trim();
            string elementName = values[1].Trim();
            string nameAttribute = values[2].Trim();
            ignoredMissingTranslations.Add(new MissingTranslation(fileName, elementName, nameAttribute));
        }

        if (!ignoredMissingTranslations.IsNullOrEmpty())
        {
            Debug.LogWarning($"Ignoring missing translations:\n  {lines.JoinWith("\n  ")}");
        }
    }

    private List<MissingTranslation> GetMissingTranslationsInUxmlFile(string uxmlFile)
    {
        List<MissingTranslation> missingTranslations = new();

        XDocument xDocument = XDocument.Parse(File.ReadAllText(uxmlFile));

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("Label", "UnityEngine.UIElements")),
            "text");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("Button", "UnityEngine.UIElements")),
            "text");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("Toggle", "UnityEngine.UIElements")),
            "label");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("TextField", "UnityEngine.UIElements")),
            "label");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("IntegerField", "UnityEngine.UIElements")),
            "label");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("FloatField", "UnityEngine.UIElements")),
            "label");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("LongField", "UnityEngine.UIElements")),
            "label");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("DoubleField", "UnityEngine.UIElements")),
            "label");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("DropdownField", "UnityEngine.UIElements")),
            "label");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants(XName.Get("EnumField", "UnityEngine.UIElements")),
            "label");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants("ItemPicker"),
            "label");

        AddMissingTranslations(
            uxmlFile,
            missingTranslations,
            xDocument.Descendants("AccordionItem"),
            "label");

        return missingTranslations;
    }

    private void AddMissingTranslations(string uxmlFile, List<MissingTranslation> missingTranslations, IEnumerable<XElement> xElements, string attributeName)
    {
        foreach (XElement xElement in xElements)
        {
            if (TryGetMissingTranslationInElement(uxmlFile, xElement, xElement.Attribute(attributeName), out MissingTranslation missingTranslation))
            {
                missingTranslations.Add(missingTranslation);
            }
        }
    }

    private bool TryGetMissingTranslationInElement(string uxmlFile, XElement xElement, XAttribute xAttribute, out MissingTranslation missingTranslation)
    {
        if (xElement == null
            || xElement.Attribute("name") == null
            || xAttribute == null)
        {
            missingTranslation = new MissingTranslation();
            return false;
        }

        if (!xAttribute.Value.StartsWith(Translation.TranslationKeyPrefix))
        {
            missingTranslation = new MissingTranslation(uxmlFile, xElement, xAttribute);
            return true;
        }

        missingTranslation = new MissingTranslation();
        return false;
    }

    private static List<string> GetFilesInFolderRecursive(string folderPath, params string[] fileExtensions)
    {
        return DirectoryUtils.GetFiles(folderPath, true, fileExtensions)
            .ToList();
    }

    private struct MissingTranslation
    {
        public string File { get; private set; }
        public string ElementLocalName { get; private set; }
        public string NameAttributeValue { get; private set; }
        public string UntranslatedAttributeName { get; private set; }
        public string UntranslatedAttributeValue { get; private set; }

        public MissingTranslation(string file, string xElementLocalName, string nameAttributeValue)
        {
            File = file;
            ElementLocalName = xElementLocalName;
            NameAttributeValue = nameAttributeValue;
            UntranslatedAttributeName = "";
            UntranslatedAttributeValue = "";
        }

        public MissingTranslation(string file, XElement xElement, XAttribute untranslatedAttribute)
            : this(file, xElement.Name.LocalName, xElement.Attribute("name").Value)
        {
            UntranslatedAttributeName = untranslatedAttribute.Name.LocalName;
            UntranslatedAttributeValue = untranslatedAttribute.Value;
        }

        public override string ToString()
        {
            return ToUxmlString();
        }

        public string ToUxmlString()
        {
            return !UntranslatedAttributeName.IsNullOrEmpty()
                ? $"<{ElementLocalName} name=\"{NameAttributeValue}\" {UntranslatedAttributeName}=\"{UntranslatedAttributeValue}\"/>"
                : $"<{ElementLocalName} name=\"{NameAttributeValue}\"/>";
        }
    }
}
