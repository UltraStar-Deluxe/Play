using System;
using System.Collections.Generic;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.Fluent.Predicates;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public class ArchUnitTests
{
    /**
     * Checks that UI label setters (e.g. UnityEngine.UIElements.BaseField.label) are not called directly.
     * Instead, a custom extension method that takes a translation object as input should be used.
     */
    // [Test]
    // [Ignore("Not all label assignments refactored yet to use a translation object with custom extension method")]
    // public void UiLabelsAreNotAssignedDirectly()
    // {
    //     Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(new List<string>()
    //     {
    //         // "playshared",
    //         // "playsharedui",
    //         "Common",
    //         // "Scenes",
    //         // "SongEditorScene",
    //     });
    //
    //     // Types().That().Are(typeof(GameRoundModifierDialogEntryControl))
    //     Types()
    //         .Should().FollowCustomCondition(NotCallLabelSetter())
    //         .Check(architecture);
    // }

    [Test]
    public void CommonOnlineMultiplayerDoesNotReferenceSpecificOnlineMultiplayer()
    {
        Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(ArchUnitTestUtils.gameSpecificRuntimeAssemblyNames);

        IObjectProvider<IType> commonOnlineMultiplayerTypes = Types().That().ResideInNamespace("CommonOnlineMultiplayer");
        IObjectProvider<IType> steamOnlineMultiplayerTypes = Types().That().ResideInNamespace("SteamOnlineMultiplayer");

        Classes().That().Are(commonOnlineMultiplayerTypes)
            .Should().NotDependOnAny(steamOnlineMultiplayerTypes)
            .Check(architecture);
    }

    [Test]
    public void ClassesThatUseInjectAttributeShouldImplementINeedInjection()
    {
        Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(ArchUnitTestUtils.gameSpecificRuntimeAssemblyNames);

        // Classes that make use of Inject attribute
        Classes().That().FollowCustomPredicate(HaveMemberWithAttribute("UniInject.InjectAttribute"))
        // Should implement INeedInjection interface
        .Should().ImplementInterface("UniInject.INeedInjection")
        // Run check on Architecture
        .Check(architecture);
    }

    private static ICondition<IType> NotCallLabelSetter()
    {
        return new SimpleCondition<IType>(type =>
            {
                return new ConditionResult(type, !type.GetCalledMethods().AnyMatch(IsLabelSetter));
            },
            $"not call label setter");
    }

    private static bool IsLabelSetter(MethodMember methodMember)
    {
        return string.Equals(methodMember.Name, "set_label(System.String)", StringComparison.InvariantCultureIgnoreCase);
    }

    private static IPredicate<Class> HaveMemberWithAttribute(string attributeFullName)
    {
        return new SimplePredicate<Class>(
            clazz => clazz.Members.AnyMatch(member => member.HasAttribute(attributeFullName)),
            $"have attribute with name {attributeFullName}");
    }
}
