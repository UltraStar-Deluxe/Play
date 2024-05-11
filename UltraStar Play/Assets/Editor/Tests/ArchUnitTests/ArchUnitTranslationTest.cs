using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using ProTrans;
using UnityEngine;
using UnityEngine.UIElements;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;
using Type = System.Type;

public class ArchUnitTranslationTest
{
    private static List<TranslatableAssignment> ignoredUntranslatedAssignments = new();
    private static HashSet<string> ignoredUntranslatedEnums = new();

    private static readonly HashSet<string> typesWithUiLabels = new()
    {
        "TextElement",
        "BaseField`1",
        "EnumFieldChooser",
        "Chooser",
        "AccordionItem",
        "SongEditorSideBarGroup",
    };

    /**
     * Checks that UI label setters (e.g. UnityEngine.UIElements.BaseField.label) are not called directly.
     * Instead, a custom extension method that takes a translation result as input should be used.
     */
    [Test]
    [TestCase("playshared")]
    [TestCase("playsharedui")]
    [TestCase("Common")]  // Common takes long to run (few minutes).
    [TestCase("Scenes")]
    [TestCase("SongEditorScene")]
    // [Ignore("Not all label assignments refactored yet to use a translation object via custom extension method")]
    public void UiLabelAssignmentsAreTranslated(string assemblyName)
    {
        LoadIgnoredUntranslatedAssignments();
        Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(new List<string>()
        {
            assemblyName
        });

        Types()
            .Should().FollowCustomCondition(NotCallUntranslatedUiLabelSetter())
            .Check(architecture);
    }

    [Test]
    [TestCase("playshared")]
    [TestCase("playsharedui")]
    [TestCase("Common")] // Common takes long to run (few minutes).
    [TestCase("Scenes")]
    [TestCase("SongEditorScene")]
    public void EnumsAreTranslated(string assemblyName)
    {
        Translation.InitTranslationConfig();
        LoadIgnoredUntranslatedEnums();
        Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(new List<string>()
        {
            assemblyName
        });

        HashSet<string> missingTranslations = new();
        Types().That().AreEnums()
            .Should().FollowCustomCondition(Do(type =>
            {
                if (IsIgnoredEnum(type))
                {
                    return;
                }

                List<IMember> membersWithoutTranslation = type.Members
                    .Where(member => member.Name != "value__")
                    .Where(member => !Translation.TryGet(GetEnumTranslationKey(member), new Dictionary<string, string>(), out Translation _))
                    .ToList();
                missingTranslations.AddRange(membersWithoutTranslation.Select(member => $"{GetEnumTranslationKey(member)}={StringUtils.ToTitleCase(member.Name)}"));
            }))
            .Check(architecture);

        if (!missingTranslations.IsNullOrEmpty())
        {
            Assert.Fail($"Missing enum translations:\n    {missingTranslations.OrderBy(it => it).JoinWith("\n    ")}");
        }
    }

    [Test]
    public void EnumTranslationHasCorrespondingEnumType()
    {
        Translation.InitTranslationConfig();
        // Must load all assemblies to find all enum types
        Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(new List<string>()
        {
            "playshared",
            "playsharedui",
            "Common",
            "Scenes",
            "SongEditorScene",
        });

        // Find all enums types
        List<IType> enums = new List<IType>();
        Types().That().AreEnums()
            .Should().FollowCustomCondition(Do(type => enums.Add(type)))
            .Check(architecture);

        // Find all valid enum translation keys
        HashSet<string> enumTranslationKeys = enums
            .SelectMany(type => type.Members)
            .Select(member => GetEnumTranslationKey(member))
            .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        // Check that actual enum translation keys are present in valid enum translation keys
        PropertiesFile defaultPropertiesFile = Translation.GetPropertiesFile(Translation.GetFallbackCultureInfo());
        List<string> translationKeysWithoutCorrespondingEnum = defaultPropertiesFile.Dictionary.Keys
            .Where(translationKey => translationKey.StartsWith("enum_")
                                     && !enumTranslationKeys.Contains(translationKey))
            .ToList();

        List<string> translationKeysWithCorrespondingEnum = defaultPropertiesFile.Dictionary.Keys
            .Except(translationKeysWithoutCorrespondingEnum)
            .ToList();

        Debug.Log($"Enum translations with corresponding enum type:\n    "
                  + translationKeysWithCorrespondingEnum.JoinWith("\n    "));
        if (!translationKeysWithoutCorrespondingEnum.IsNullOrEmpty())
        {
            Assert.Fail($"Enum translations without corresponding enum type:\n    "
                        + translationKeysWithoutCorrespondingEnum.JoinWith("\n    "));
        }
    }

    [Test]
    public void UntranslatedUiLabelAssignmentsAreFound()
    {
        Architecture architecture = new ArchLoader()
            .LoadAssemblies(Assembly.GetAssembly(typeof(UntranslatedButtonTextSetterExample)))
            .Build();

        List<Type> types = new List<Type>(){
            typeof(UntranslatedButtonTextSetterExample),
            typeof(UntranslatedLabelTextSetterExample),
            typeof(UntranslatedTextFieldLabelSetterExample),
            typeof(UntranslatedChooserLabelSetterExample),
            typeof(UntranslatedAccordionItemTitleSetterExample),
            typeof(UntranslatedSongEditorSideBarGroupLabelSetterExample),
        };

        foreach (Type type in types)
        {
            Assert.Throws<AssertionException>(() =>
                    Types().That().Are(type)
                        .Should().FollowCustomCondition(NotCallUntranslatedUiLabelSetter())
                        .Check(architecture),
                $"Untranslated UI label assignment was not found in type {type.FullName}");
        }
    }

    private static ICondition<IType> Do(Action<IType> action)
    {
        return new SimpleCondition<IType>(type =>
        {
            action(type);
            return new ConditionResult(type, true, "do action");
        }, "do action");
    }

    private static bool IsIgnoredEnum(IType type)
    {
        return ignoredUntranslatedEnums.Contains(type.Name)
            || (type.Name.StartsWith('E') && ignoredUntranslatedEnums.Contains(type.Name.Substring(1)));
    }

    private static string GetEnumTranslationKey(IMember enumValue)
    {
        string typeName = enumValue.DeclaringType.Name;
        string valueName = enumValue.Name;

        return typeName.StartsWith("E")
            ? $"enum_{typeName.Substring(1)}_{valueName}"
            : $"enum_{typeName}_{valueName}";
    }

    private static ICondition<IType> NotCallUntranslatedUiLabelSetter()
    {
        return new SimpleCondition<IType>(type =>
            {
                List<MethodMember> calledUiLabelSetters = Enumerable.ToList(Enumerable.Where(Enumerable.Where(type.GetCalledMethods(), IsUiLabelSetter), method => !IsIgnoredTranslatableAssignment(type, method)));
                string calledUiLabelSettersCsv = Enumerable.Select(calledUiLabelSetters, method => $"{method.DeclaringType.Name}.{method.Name}").JoinWith(", ");
                return new ConditionResult(type, calledUiLabelSetters.IsNullOrEmpty(), $"should not call untranslated UI label setter {calledUiLabelSettersCsv}");
            },
            $"not call untranslated UI label setter");
    }

    private static bool IsIgnoredTranslatableAssignment(IType type, MethodMember method)
    {
        return ignoredUntranslatedAssignments.AnyMatch(ignoredAssignment =>
        {
            return (ignoredAssignment.TypeFullName == "*"
                    || string.Equals(ignoredAssignment.TypeFullName, type.FullName, StringComparison.InvariantCultureIgnoreCase))
                   && (ignoredAssignment.MethodDeclaringTypeName == "*"
                       || string.Equals(ignoredAssignment.MethodDeclaringTypeName, method.DeclaringType.Name, StringComparison.InvariantCultureIgnoreCase))
                   && (ignoredAssignment.MethodName == "*"
                       || string.Equals(ignoredAssignment.MethodName, method.Name, StringComparison.InvariantCultureIgnoreCase));
        });
    }

    private static bool IsUiLabelSetter(MethodMember methodMember)
    {
        return typesWithUiLabels.Contains(methodMember.DeclaringType.Name)
               && (string.Equals(methodMember.Name, "set_label(System.String)", StringComparison.InvariantCultureIgnoreCase)
                   || string.Equals(methodMember.Name, "set_text(System.String)", StringComparison.InvariantCultureIgnoreCase)
                   || string.Equals(methodMember.Name, "set_title(System.String)", StringComparison.InvariantCultureIgnoreCase));
    }

    private static void LoadIgnoredUntranslatedAssignments()
    {
        ignoredUntranslatedAssignments = new();

        string[] lines = File.ReadAllLines("Assets/Editor/Tests/ArchUnitTests/IgnoredUntranslatedAssignments.csv");
        if (lines.Length > 1)
        {
            Debug.LogWarning($"Ignoring untranslated assignments:\n  {lines.JoinWith("\n  ")}");
        }

        // Start at index 1 to skip header line
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(",");
            string typeName = values[0].Trim();
            string methodDeclaringTypeName = values[1].Trim();
            string methodName = values[2].Trim();
            ignoredUntranslatedAssignments.Add(new TranslatableAssignment(typeName, methodDeclaringTypeName, methodName));
        }
    }

    private static void LoadIgnoredUntranslatedEnums()
    {
        string[] lines = File.ReadAllLines("Assets/Editor/Tests/ArchUnitTests/IgnoredUntranslatedEnums.csv");
        if (lines.Length > 1)
        {
            Debug.LogWarning($"Ignoring untranslated assignments:\n  {lines.JoinWith("\n  ")}");
        }
        // Start at index 1 to skip header line
        ignoredUntranslatedEnums = Enumerable.ToHashSet(Enumerable.Select(Enumerable.Skip(lines, 1), it => it.Trim()));
    }

    private struct TranslatableAssignment
    {
        public string TypeFullName { get; private set; }
        public string MethodDeclaringTypeName { get; private set; }
        public string MethodName { get; private set; }

        public TranslatableAssignment(string typeFullName, string methodDeclaringTypeName, string methodName)
        {
            TypeFullName = typeFullName;
            MethodDeclaringTypeName = methodDeclaringTypeName;
            MethodName = methodName;
        }
    }

    private struct UntranslatedButtonTextSetterExample
    {
        private static void Init()
        {
            new Button().text = "untranslated text";
        }
    }

    private struct UntranslatedLabelTextSetterExample
    {
        private static void Init()
        {
            new Label().text = "untranslated text";
        }
    }

    private struct UntranslatedTextFieldLabelSetterExample
    {
        private static void Init()
        {
            new TextField().label = "untranslated text";
        }
    }

    private struct UntranslatedChooserLabelSetterExample
    {
        private static void Init()
        {
            new Chooser().Label = "untranslated text";
        }
    }

    private struct UntranslatedAccordionItemTitleSetterExample
    {
        private static void Init()
        {
            new AccordionItem().Title = "untranslated text";
        }
    }

    private struct UntranslatedSongEditorSideBarGroupLabelSetterExample
    {
        private static void Init()
        {
            new SongEditorSideBarGroup().Label = "untranslated text";
        }
    }
}
