using NUnit.Framework;
using UniInject;

// Disable warning about never assigned fields. The values are injected.
#pragma warning disable CS0649

namespace UniInject.Tests
{
    public class ConstructorInjectionTests
    {
        [Test]
        public void ConstructorInjectionFromExistingInstance()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.BindExistingInstance("abc");

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsConstructorInjection needsInjection = injector.Create<NeedsConstructorInjection>();
            Assert.AreEqual("abc", needsInjection.theString);
        }

        [Test]
        public void ConstructorInjectionWithMultipleParameters()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.BindExistingInstance("abc");
            bb.BindExistingInstance(123);

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsConstructorInjectionWithMultipleParameters needsInjection = injector.Create<NeedsConstructorInjectionWithMultipleParameters>();
            Assert.AreEqual("abc", needsInjection.theString);
            Assert.AreEqual(123, needsInjection.theInt);
        }

        [Test]
        public void ConstructorInjectionWithCustomKey()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.Bind("author").ToExistingInstance("Tolkien");

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsConstructorInjectionWithCustomKey needsInjection = injector.Create<NeedsConstructorInjectionWithCustomKey>();
            Assert.AreEqual("Tolkien", needsInjection.theAuthor);
        }

        [Test]
        public void ConstructorInjectionWithAcyclicDependencies()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.BindTypeToNewInstances(typeof(NeedsConstructorInjectionWithAcyclicDependencies_A));
            bb.BindTypeToNewInstances(typeof(NeedsConstructorInjectionWithAcyclicDependencies_B));
            bb.BindTypeToNewInstances(typeof(NeedsConstructorInjectionWithAcyclicDependencies_C));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsConstructorInjectionWithAcyclicDependencies_A aInstance = injector.Create<NeedsConstructorInjectionWithAcyclicDependencies_A>();
            // The dependency in A (requires B) and B (requires C) must have been resolved with new instances.
            Assert.NotNull(aInstance.bInstance);
            Assert.NotNull(aInstance.bInstance.cInstance);
        }

        [Test]
        public void ConstructorInjectionWithCyclicDependenciesThrowsException()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.BindTypeToNewInstances(typeof(NeedsConstructorInjectionWithCyclicDependencies_A));
            bb.BindTypeToNewInstances(typeof(NeedsConstructorInjectionWithCyclicDependencies_B));
            bb.BindTypeToNewInstances(typeof(NeedsConstructorInjectionWithCyclicDependencies_C));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            Assert.Throws<CyclicConstructorDependenciesException>(delegate { injector.Create<NeedsConstructorInjectionWithCyclicDependencies_A>(); });
        }

        /////////////////////////////////////////////////////////////////
        private class NeedsConstructorInjection
        {
            public string theString;

            public NeedsConstructorInjection(string s)
            {
                this.theString = s;
            }
        }

        private class NeedsConstructorInjectionWithMultipleParameters
        {
            public string theString;
            public int theInt;

            public NeedsConstructorInjectionWithMultipleParameters(string s, int i)
            {
                this.theString = s;
                this.theInt = i;
            }
        }

        private class NeedsConstructorInjectionWithCustomKey
        {
            public string theAuthor;

            public NeedsConstructorInjectionWithCustomKey([InjectionKey("author")] string author)
            {
                this.theAuthor = author;
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // Acyclic
        private class NeedsConstructorInjectionWithAcyclicDependencies_A
        {
            public NeedsConstructorInjectionWithAcyclicDependencies_B bInstance;

            public NeedsConstructorInjectionWithAcyclicDependencies_A(NeedsConstructorInjectionWithAcyclicDependencies_B bInstance)
            {
                this.bInstance = bInstance;
            }
        }

        private class NeedsConstructorInjectionWithAcyclicDependencies_B
        {
            public NeedsConstructorInjectionWithAcyclicDependencies_C cInstance;

            public NeedsConstructorInjectionWithAcyclicDependencies_B(NeedsConstructorInjectionWithAcyclicDependencies_C cInstance)
            {
                this.cInstance = cInstance;
            }
        }

        private class NeedsConstructorInjectionWithAcyclicDependencies_C
        {
            public NeedsConstructorInjectionWithAcyclicDependencies_C()
            {
            }
        }

        //////////////////////////////////////////////////////////////////////
        // Cyclic (not allowed)
        private class NeedsConstructorInjectionWithCyclicDependencies_A
        {
            public NeedsConstructorInjectionWithCyclicDependencies_B bInstance;

            public NeedsConstructorInjectionWithCyclicDependencies_A(NeedsConstructorInjectionWithCyclicDependencies_B bInstance)
            {
                this.bInstance = bInstance;
            }
        }

        private class NeedsConstructorInjectionWithCyclicDependencies_B
        {
            public NeedsConstructorInjectionWithCyclicDependencies_C cInstance;

            public NeedsConstructorInjectionWithCyclicDependencies_B(NeedsConstructorInjectionWithCyclicDependencies_C cInstance)
            {
                this.cInstance = cInstance;
            }
        }

        private class NeedsConstructorInjectionWithCyclicDependencies_C
        {
            public NeedsConstructorInjectionWithCyclicDependencies_A aInstance;

            public NeedsConstructorInjectionWithCyclicDependencies_C(NeedsConstructorInjectionWithCyclicDependencies_A aInstance)
            {
                this.aInstance = aInstance;
            }
        }
    }
}