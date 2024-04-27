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
using UnityEngine;
using UnityEngine.UIElements;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;
using Type = System.Type;

public class ArchUnitTranslationTests
{
    private static List<TranslatableAssignment> ignoredUntranslatedAssignments;

    private static readonly HashSet<string> typesWithUiLabels = new()
    {
        "TextElement",
        "BaseField`1",
        "EnumFieldItemPicker",
        "ItemPicker",
        "AccordionItem",
    };

    /**
     * Checks that UI label setters (e.g. UnityEngine.UIElements.BaseField.label) are not called directly.
     * Instead, a custom extension method that takes a translation result as input should be used.
     */
    [Test]
    // [Ignore("Not all label assignments refactored yet to use a translation object via custom extension method")]
    public void UiLabelAssignmentsAreTranslated()
    {
        LoadIgnoredUntranslatedAssignments();
        Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(new List<string>()
        {
            "playsharedui",
            // "Common", // Common takes pretty long to run. The other assemblies finish the test faster.
            // "Scenes",
            // "SongEditorScene",
        });

        Types()
            .Should().FollowCustomCondition(NotCallUntranslatedUiLabelSetter())
            .Check(architecture);
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
            typeof(UntranslatedItemPickerLabelSetterExample),
            typeof(UntranslatedAccordionItemTitleSetterExample),
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

    private static ICondition<IType> NotCallUntranslatedUiLabelSetter()
    {
        return new SimpleCondition<IType>(type =>
            {
                List<MethodMember> calledUiLabelSetters = type.GetCalledMethods()
                    .Where(IsUiLabelSetter)
                    .Where(method => !IsIgnoredTranslatableAssignment(type, method))
                    .ToList();
                string calledUiLabelSettersCsv = calledUiLabelSetters.Select(method => $"{method.DeclaringType.Name}.{method.Name}").JoinWith(", ");
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

        if (!ignoredUntranslatedAssignments.IsNullOrEmpty())
        {
            Debug.LogWarning($"Ignoring untranslated assignments:\n  {lines.JoinWith("\n  ")}");
        }
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

    private struct UntranslatedItemPickerLabelSetterExample
    {
        private static void Init()
        {
            new ItemPicker().Label = "untranslated text";
        }
    }

    private struct UntranslatedAccordionItemTitleSetterExample
    {
        private static void Init()
        {
            new AccordionItem().Title = "untranslated text";
        }
    }
}
