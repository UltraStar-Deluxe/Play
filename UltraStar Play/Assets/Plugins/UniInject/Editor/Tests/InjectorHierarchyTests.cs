using NUnit.Framework;
using UniInject;

namespace UniInject.Tests
{
    public class InjectorHierarchyTests
    {
        [Test]
        public void ParentInjectorBindingsAreVisibleToChildren()
        {
            Injector parentInjector = UniInjectUtils.CreateInjector();
            Injector childInjector = UniInjectUtils.CreateInjector(parentInjector);

            parentInjector.AddBindingForInstance("abc");
            // The string is bound in the parent and the child can use this binding.
            object valueForString = childInjector.GetValueForInjectionKey(typeof(string));

            Assert.AreEqual("abc", valueForString);
        }

        [Test]
        public void ChildrenInjectorBindingsAreNotVisibleToParents()
        {
            Injector parentInjector = UniInjectUtils.CreateInjector();
            Injector childInjector = UniInjectUtils.CreateInjector(parentInjector);

            childInjector.AddBindingForInstance("abc");
            // The string is bound in the child and the parent can not use this binding.
            Assert.Throws<MissingBindingException>(delegate { parentInjector.GetValueForInjectionKey(typeof(string)); });
        }

        [Test]
        public void GlobalInjectorIsParentOfNewInjectors()
        {
            Injector injector = UniInjectUtils.CreateInjector();

            Assert.AreEqual(UniInjectUtils.GlobalInjector, injector.ParentInjector);
        }
    }

}