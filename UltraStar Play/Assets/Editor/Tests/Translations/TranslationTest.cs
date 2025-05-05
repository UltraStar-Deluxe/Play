using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AhoCorasick;
using NUnit.Framework;
using ProTrans;
using UnityEngine;

[Ignore("Manual test")]
public class TranslationTest
{
    private List<TranslatableAttribute> ignoredMissingTranslations;

    [SetUp]
    public void SetUp()
    {
        Translation.InitTranslationConfig();
    }

    [Test]
    public void ShouldNotHaveUnusedTranslationKeys()
    {
        PropertiesFile defaultPropertiesFile = Translation.GetPropertiesFile(Translation.GetFallbackCultureInfo());
        ShouldNotHaveUnusedTranslationKeys(defaultPropertiesFile.Dictionary.Keys.ToHashSet());
    }

    [Test]
    public void ShouldFindUnusedTranslationKeys()
    {
        HashSet<string> translationKeys = new HashSet<string>() { "this_is_not_used" };
        Assert.Throws<AssertionException>(
            () => ShouldNotHaveUnusedTranslationKeys(translationKeys),
            "Did not find unused translation");
    }

    private void ShouldNotHaveUnusedTranslationKeys(HashSet<string> translationKeys)
    {

        HashSet<string> ignoredFileNames = new()
        {
            // Ignore file with generated constants for translation keys.
            "RMessages.cs",
            // Ignore this file itself
            "TranslationTest.cs",
        };

        HashSet<string> unseenTranslationKeys = new();
        HashSet<string> seenTranslationKeys = new();

        // Ignore translations for companion app
        // and ignore translations that are used dynamically via string concatenation.
        List<string> ignoredTranslationKeyPrefixes = new List<string>() { "enum_", "companionApp_", "language_"};

        // AhoCorasick search algorithm as recommended by https://stackoverflow.com/questions/46339057/c-sharp-fastest-string-search-in-all-files
        Trie trie = new();
        unseenTranslationKeys.AddRange(translationKeys
            .Where(key => !ignoredTranslationKeyPrefixes.AnyMatch(prefix => key.StartsWith(prefix))));
        unseenTranslationKeys.ForEach(key => trie.Add(key));
        trie.Build();

        // Search in Assets and Packages for .cs and .uxml files
        List<string> sourceFolders = new List<string>() { Application.dataPath, $"{Application.dataPath}/../Packages" }
            .Select(folder => new DirectoryInfo(folder).FullName)
            .ToList();
        List<string> files = FileScanner.GetFiles(sourceFolders, new FileScannerConfig("*.cs", "*.uxml") { Recursive = true });

        foreach (string file in files)
        {
            if (ignoredFileNames.Contains(Path.GetFileName(file)))
            {
                continue;
            }

            string fileContent = File.ReadAllText(file);
            foreach (string match in trie.Find(fileContent))
            {
                unseenTranslationKeys.Remove(match);
                seenTranslationKeys.Add(match);
            }

            if (unseenTranslationKeys.IsNullOrEmpty())
            {
                break;
            }
        }

        Debug.Log($"Used translation keys:\n    {seenTranslationKeys.OrderBy(it => it).JoinWith("\n    ")}");
        if (!unseenTranslationKeys.IsNullOrEmpty())
        {
            Assert.Fail($"Unused translation keys:\n    {unseenTranslationKeys.OrderBy(it => it).JoinWith("\n    ")}");
        }
    }

    [Test]
    public void ShouldNotHaveDuplicateTranslationValues()
    {
        // TODO: Remove duplicate translation values where it makes sense
        PropertiesFile defaultPropertiesFile = Translation.GetPropertiesFile(Translation.GetFallbackCultureInfo());
        foreach (KeyValuePair<string,string> entry in defaultPropertiesFile.Dictionary)
        {
            List<KeyValuePair<string, string>> duplicateEntries = defaultPropertiesFile.Dictionary
                .Where(otherEntry => otherEntry.Key != entry.Key && otherEntry.Value == entry.Value)
                .ToList();
            if (!duplicateEntries.IsNullOrEmpty())
            {
                Debug.LogWarning($"Duplicate translation values: {entry.Key} & {duplicateEntries.Select(it => it.Key).JoinWith(" & ")} = {entry.Value}");
            }
        }
    }

    [Test]
    public void AllTranslationKeysArePresentInDefaultPropertiesFiles()
    {
        Dictionary<PropertiesFile, List<string>> propertiesFileToKeys = new();

        PropertiesFile defaultPropertiesFile = Translation.GetPropertiesFile(Translation.GetFallbackCultureInfo());
        Translation.GetTranslatedCultureInfos()
            .ForEach(cultureInfo =>
            {
                PropertiesFile propertiesFile = Translation.GetPropertiesFile(cultureInfo);
                if (propertiesFile == null)
                {
                    return;
                }

                List<string> keys = propertiesFile.Dictionary.Keys
                    .Where(key => !defaultPropertiesFile.Dictionary.ContainsKey(key))
                    .ToList();
                if (!keys.IsNullOrEmpty())
                {
                    propertiesFileToKeys.Add(propertiesFile, keys);
                }
            });

        if (!propertiesFileToKeys.IsNullOrEmpty())
        {
            Assert.Fail("Found keys not present in default properties files:\n"
             + propertiesFileToKeys.Keys
                 .Select(propertiesFile =>
                 {
                     List<string> keys = propertiesFileToKeys[propertiesFile];
                     return $"{propertiesFile.CultureInfo}\n    {keys.JoinWith("\n    ")}";
                 })
                 .JoinWith("\n"));
        }
    }

    [Test]
    public void UxmlFilesAreTranslated()
    {
        List<TranslatableAttribute> allMissingTranslations = GetAllTranslatableAttributes()
            .Where(translatableAttribute => IsMissingTranslation(translatableAttribute)
                                            && !IsIgnoredMissingTranslation(translatableAttribute))
            .ToList();

        AssertTranslatableAttributesAreEmpty(allMissingTranslations, "missing translations");
    }

    [Test]
    public void UxmlFilesUseExistingTranslationKeys()
    {
        List<TranslatableAttribute> allInvalidTranslations = GetAllTranslatableAttributes()
            .Where(translatableAttribute => IsInvalidTranslation(translatableAttribute))
            .ToList();

        AssertTranslatableAttributesAreEmpty(allInvalidTranslations, "invalid translation keys");
    }

    private bool IsMissingTranslation(TranslatableAttribute translatableAttribute)
    {
        string translationKeyWithPrefix = translatableAttribute.TranslatableAttributeValue;
        if (translationKeyWithPrefix.IsNullOrEmpty())
        {
            // Ignore
            return false;
        }

        return !translatableAttribute.TranslatableAttributeValue.StartsWith(Translation.TranslationKeyPrefix);
    }

    private bool IsInvalidTranslation(TranslatableAttribute translatableAttribute)
    {
        string translationKeyWithPrefix = translatableAttribute.TranslatableAttributeValue;
        if (translationKeyWithPrefix.IsNullOrEmpty()
            || !translationKeyWithPrefix.StartsWith(Translation.TranslationKeyPrefix))
        {
            // Not using a translation key
            return false;
        }

        string translationKey = translationKeyWithPrefix.Substring(1);

        TranslationConfig.Singleton.MissingPlaceholderStrategy = MissingPlaceholderStrategy.Ignore;
        return !Translation.TryGet(translationKey, null, out Translation _);
    }

    private List<TranslatableAttribute> GetAllTranslatableAttributes()
    {
        List<string> uxmlFilesInAssets = GetFilesInFolderRecursive("Assets", "*.uxml")
            .ToList();
        List<string> uxmlFilesInPlayShared = GetFilesInFolderRecursive("Packages/playsharedui/Runtime", "*.uxml")
            .ToList();
        List<string> uxmlFiles = uxmlFilesInPlayShared
            .Union(uxmlFilesInAssets)
            .Where(uxmlFile => !IsIgnoredFile(uxmlFile))
            .ToList();

        return uxmlFiles
            .SelectMany(uxmlFile => GetTranslatableAttributesInUxmlFile(uxmlFile))
            .ToList();
    }

    private void AssertTranslatableAttributesAreEmpty(
        List<TranslatableAttribute> allTranslatableAttributes,
        string errorMessagePrefix)
    {
        Dictionary<string, List<TranslatableAttribute>> uxmlFileToTranslatableAttributes = allTranslatableAttributes
            .GroupBy(it => it.File)
            .ToDictionary(it => it.Key, it => it.ToList());

        uxmlFileToTranslatableAttributes.Keys.ForEach(uxmlFile =>
        {
            List<TranslatableAttribute> translatableAttributes = uxmlFileToTranslatableAttributes[uxmlFile];
            Debug.LogError($"{errorMessagePrefix} in file: {Path.GetFileName(uxmlFile)}\n    "
                           + translatableAttributes.Select(it => it.ToUxmlString())
                               .JoinWith("\n    "));
        });

        Assert.IsEmpty(allTranslatableAttributes, $"{errorMessagePrefix} in UXML files");
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

    private bool IsIgnoredMissingTranslation(TranslatableAttribute translatableAttribute)
    {
        if (ignoredMissingTranslations.IsNullOrEmpty())
        {
            LoadIgnoredMissingTranslations();
        }

        string missingTranslationFileName = Path.GetFileName(translatableAttribute.File);
        return ignoredMissingTranslations.AnyMatch(ignoredMissingTranslation =>
        {
            string ignoredMissingTranslationFileName = Path.GetFileName(ignoredMissingTranslation.File);
            return (ignoredMissingTranslationFileName == "*"
                    || string.Equals(ignoredMissingTranslationFileName, missingTranslationFileName, StringComparison.InvariantCultureIgnoreCase))
                   && (ignoredMissingTranslation.ElementLocalName == "*"
                       || string.Equals(ignoredMissingTranslation.ElementLocalName, translatableAttribute.ElementLocalName, StringComparison.InvariantCultureIgnoreCase))
                   && (ignoredMissingTranslation.NameAttributeValue == "*"
                       || string.Equals(ignoredMissingTranslation.NameAttributeValue, translatableAttribute.NameAttributeValue, StringComparison.InvariantCultureIgnoreCase));
        });
    }

    private void LoadIgnoredMissingTranslations()
    {
        ignoredMissingTranslations = new();

        string[] lines = File.ReadAllLines("Assets/Editor/Tests/Translations/IgnoredMissingTranslations.csv");
        if (lines.Length > 1)
        {
            Debug.LogWarning($"Ignoring missing translations:\n  {lines.JoinWith("\n  ")}");
        }

        // Start at index 1 to skip header line
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(",");
            string fileName = values[0].Trim();
            string elementName = values[1].Trim();
            string nameAttribute = values[2].Trim();
            ignoredMissingTranslations.Add(new TranslatableAttribute(fileName, elementName, nameAttribute));
        }
    }

    private List<TranslatableAttribute> GetTranslatableAttributesInUxmlFile(string uxmlFile)
    {
        List<TranslatableAttribute> translatableAttributes = new();

        XDocument xDocument = XDocument.Parse(File.ReadAllText(uxmlFile));

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("Label", "UnityEngine.UIElements")),
            "text");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("Button", "UnityEngine.UIElements")),
            "text");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("Toggle", "UnityEngine.UIElements")),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("TextField", "UnityEngine.UIElements")),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("IntegerField", "UnityEngine.UIElements")),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("FloatField", "UnityEngine.UIElements")),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("LongField", "UnityEngine.UIElements")),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("DoubleField", "UnityEngine.UIElements")),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("DropdownField", "UnityEngine.UIElements")),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants(XName.Get("EnumField", "UnityEngine.UIElements")),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants("Chooser"),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants("SongEditorSideBarGroup"),
            "label");

        AddTranslatableAttributes(
            uxmlFile,
            translatableAttributes,
            xDocument.Descendants("AccordionItem"),
            "label");

        return translatableAttributes;
    }

    private void AddTranslatableAttributes(string uxmlFile, List<TranslatableAttribute> translatableAttributes, IEnumerable<XElement> xElements, string translatableAttributeName)
    {
        foreach (XElement xElement in xElements)
        {
            if (TryGetTranslatableAttributeInElement(uxmlFile, xElement, xElement.Attribute(translatableAttributeName), out TranslatableAttribute translatableAttribute))
            {
                translatableAttributes.Add(translatableAttribute);
            }
        }
    }

    private bool TryGetTranslatableAttributeInElement(string uxmlFile, XElement xElement, XAttribute xAttribute, out TranslatableAttribute translatableAttribute)
    {
        if (xElement == null
            || xElement.Attribute("name") == null
            || xAttribute == null)
        {
            translatableAttribute = new TranslatableAttribute();
            return false;
        }

        translatableAttribute = new TranslatableAttribute(uxmlFile, xElement, xAttribute);
        return true;
    }

    private static List<string> GetFilesInFolderRecursive(string folderPath, params string[] fileExtensions)
    {
        return FileScanner.GetFiles(folderPath, new FileScannerConfig(fileExtensions) { Recursive = true })
            .ToList();
    }

    private struct TranslatableAttribute
    {
        public string File { get; private set; }
        public string ElementLocalName { get; private set; }
        public string NameAttributeValue { get; private set; }
        public string TranslatableAttributeName { get; private set; }
        public string TranslatableAttributeValue { get; private set; }

        public TranslatableAttribute(string file, string xElementLocalName, string nameAttributeValue)
        {
            File = file;
            ElementLocalName = xElementLocalName;
            NameAttributeValue = nameAttributeValue;
            TranslatableAttributeName = "";
            TranslatableAttributeValue = "";
        }

        public TranslatableAttribute(string file, XElement xElement, XAttribute untranslatedAttribute)
            : this(file, xElement.Name.LocalName, xElement.Attribute("name").Value)
        {
            TranslatableAttributeName = untranslatedAttribute.Name.LocalName;
            TranslatableAttributeValue = untranslatedAttribute.Value;
        }

        public override string ToString()
        {
            return ToUxmlString();
        }

        public string ToUxmlString()
        {
            return !TranslatableAttributeName.IsNullOrEmpty()
                ? $"<{ElementLocalName} name=\"{NameAttributeValue}\" {TranslatableAttributeName}=\"{TranslatableAttributeValue}\"/>"
                : $"<{ElementLocalName} name=\"{NameAttributeValue}\"/>";
        }
    }
}
