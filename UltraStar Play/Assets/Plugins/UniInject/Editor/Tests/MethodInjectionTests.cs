using NUnit.Framework;
using UniInject;

// Disable warning about never assigned fields. The values are injected.
#pragma warning disable CS0649

namespace UniInject.Tests
{
    public class MethodInjectionTests
    {
        [Test]
        public void MethodInjectionFromExistingInstance()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.BindExistingInstance("abc");

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsMethodInjection needsInjection = new NeedsMethodInjection();
            injector.Inject(needsInjection);
            Assert.AreEqual("abc", needsInjection.theString);
        }

        [Test]
        public void MethodInjectionFromSingleInstanceOfType()
        {
            ImplWithInstanceIndex.instanceCount = 0;

            BindingBuilder bb = new BindingBuilder();
            bb.BindTypeToSingleInstance(typeof(ImplWithInstanceIndex));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsMethodInjection needsInjection1 = injector.CreateAndInject<NeedsMethodInjection>();
            NeedsMethodInjection needsInjection2 = injector.CreateAndInject<NeedsMethodInjection>();
            NeedsMethodInjection needsInjection3 = injector.CreateAndInject<NeedsMethodInjection>();

            // Assert injection was successful
            Assert.NotNull(needsInjection1);
            Assert.NotNull(needsInjection2);
            Assert.NotNull(needsInjection3);
            // Assert only one instance was created
            Assert.AreEqual(1, needsInjection1.implWithInstanceCounter.instanceIndex);
            Assert.AreEqual(1, needsInjection2.implWithInstanceCounter.instanceIndex);
            Assert.AreEqual(1, needsInjection3.implWithInstanceCounter.instanceIndex);
            Assert.AreEqual(1, ImplWithInstanceIndex.instanceCount);
        }

        [Test]
        public void MethodInjectionFromNewInstancesOfType()
        {
            ImplWithInstanceIndex.instanceCount = 0;

            BindingBuilder bb = new BindingBuilder();
            bb.BindTypeToNewInstances(typeof(ImplWithInstanceIndex));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsMethodInjection needsInjection1 = injector.CreateAndInject<NeedsMethodInjection>();
            NeedsMethodInjection needsInjection2 = injector.CreateAndInject<NeedsMethodInjection>();
            NeedsMethodInjection needsInjection3 = injector.CreateAndInject<NeedsMethodInjection>();

            // Assert injection was successful
            Assert.NotNull(needsInjection1);
            Assert.NotNull(needsInjection2);
            Assert.NotNull(needsInjection3);
            // Assert that different instances were injected.
            Assert.AreEqual(1, needsInjection1.implWithInstanceCounter.instanceIndex);
            Assert.AreEqual(2, needsInjection2.implWithInstanceCounter.instanceIndex);
            Assert.AreEqual(3, needsInjection3.implWithInstanceCounter.instanceIndex);
            Assert.AreEqual(3, ImplWithInstanceIndex.instanceCount);
        }

        [Test]
        public void NonOptionalMethodInjectionThrowsExceptionIfNotPossible()
        {
            Injector injector = UniInjectUtils.CreateInjector();

            NeedsNonOptionalMethodInjectionNonOptional needsInjection = new NeedsNonOptionalMethodInjectionNonOptional();

            Assert.Throws<InjectionException>(delegate { injector.Inject(needsInjection); });
        }

        [Test]
        public void MethodInjectionOfInterfaceThatIsBoundToAnImplementation()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.Bind(typeof(IInstanceIndexHolder)).ToSingleInstanceOfType(typeof(ImplWithInstanceIndex));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsMethodInjectionOfInterface needsInjection = injector.CreateAndInject<NeedsMethodInjectionOfInterface>();

            Assert.NotNull(needsInjection.interfaceInstance);
        }

        [Test]
        public void MethodInjectionWithCustomKey()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.Bind("author").ToExistingInstance("Tolkien");

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsMethodInjectionWithCustomKey needsInjection = injector.CreateAndInject<NeedsMethodInjectionWithCustomKey>();

            Assert.AreEqual("Tolkien", needsInjection.theAuthor);
        }

        [Test]
        public void CyclicMethodInjection()
        {
            NeedsMethodInjectionCyclic_A.instanceCount = 0;
            NeedsMethodInjectionCyclic_B.instanceCount = 0;
            NeedsMethodInjectionCyclic_C.instanceCount = 0;

            BindingBuilder bb = new BindingBuilder();
            bb.BindTypeToNewInstances(typeof(NeedsMethodInjectionCyclic_A));
            bb.BindTypeToNewInstances(typeof(NeedsMethodInjectionCyclic_B));
            bb.BindTypeToNewInstances(typeof(NeedsMethodInjectionCyclic_C));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);
            NeedsMethodInjectionCyclic_A aInstance = injector.CreateAndInject<NeedsMethodInjectionCyclic_A>();

            // The dependency in C must have been resolved with the instance of A that was created.
            Assert.NotNull(aInstance.bInstance);
            Assert.NotNull(aInstance.bInstance.cInstance);
            Assert.NotNull(aInstance.bInstance.cInstance.aInstance);
            Assert.AreEqual(aInstance.instanceIndex, aInstance.bInstance.cInstance.aInstance.instanceIndex);
        }

        [Test]
        public void MethodInjectionWithMultipleParameters()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.BindExistingInstance("Tolkien");
            bb.BindExistingInstance(1954);
            bb.Bind(typeof(IInstanceIndexHolder)).ToExistingInstance(new ImplWithInstanceIndex());

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);
            NeedsMethodInjectionWithMultipleParameters needsInjection = injector.CreateAndInject<NeedsMethodInjectionWithMultipleParameters>();

            Assert.AreEqual("Tolkien", needsInjection.theAuthor);
            Assert.AreEqual(1954, needsInjection.theYear);
            Assert.NotNull(needsInjection.theInterfaceInstance);
        }

        private class NeedsMethodInjectionWithMultipleParameters
        {
            public string theAuthor;
            public int theYear;
            public IInstanceIndexHolder theInterfaceInstance;

            [Inject]
            public void Set(string author, int year, IInstanceIndexHolder interfaceInstance)
            {
                this.theAuthor = author;
                this.theYear = year;
                this.theInterfaceInstance = interfaceInstance;
            }
        }

        private class NeedsMethodInjectionWithCustomKey
        {
            public string theAuthor;

            [Inject]
            public void Set([InjectionKey("author")] string author)
            {
                this.theAuthor = author;
            }
        }

        private class NeedsMethodInjectionCyclic_A
        {
            public static int instanceCount;
            public readonly int instanceIndex;

            public NeedsMethodInjectionCyclic_B bInstance;

            public NeedsMethodInjectionCyclic_A()
            {
                instanceCount++;
                instanceIndex = instanceCount;
            }

            [Inject]
            public void Set(NeedsMethodInjectionCyclic_B bInstance)
            {
                this.bInstance = bInstance;
            }
        }

        private class NeedsMethodInjectionCyclic_B
        {
            public static int instanceCount;
            public readonly int instanceIndex;

            public NeedsMethodInjectionCyclic_C cInstance;

            public NeedsMethodInjectionCyclic_B()
            {
                instanceCount++;
                instanceIndex = instanceCount;
            }

            [Inject]
            public void Set(NeedsMethodInjectionCyclic_C cInstance)
            {
                this.cInstance = cInstance;
            }
        }

        private class NeedsMethodInjectionCyclic_C
        {
            public static int instanceCount;
            public readonly int instanceIndex;

            public NeedsMethodInjectionCyclic_A aInstance;

            public NeedsMethodInjectionCyclic_C()
            {
                instanceCount++;
                instanceIndex = instanceCount;
            }

            [Inject]
            public void Set(NeedsMethodInjectionCyclic_A aInstance)
            {
                this.aInstance = aInstance;
            }
        }

        private class NeedsNonOptionalMethodInjectionNonOptional
        {
            public string theString;

            [Inject]
            public void Set(string s)
            {
                this.theString = s;
            }
        }

        private class NeedsMethodInjectionOfInterface
        {
            public IInstanceIndexHolder interfaceInstance;

            [Inject]
            public void Set(IInstanceIndexHolder interfaceInstance)
            {
                this.interfaceInstance = interfaceInstance;
            }
        }

        private class NeedsMethodInjection
        {
            public string theString;
            public ImplWithInstanceIndex implWithInstanceCounter;

            [Inject(optional = true)]
            public void SetImpl(ImplWithInstanceIndex impl)
            {
                this.implWithInstanceCounter = impl;
            }

            [Inject(optional = true)]
            public void SetString(string s)
            {
                this.theString = s;
            }
        }

        private interface IInstanceIndexHolder
        {
            int GetInstanceIndex();
        }

        private class ImplWithInstanceIndex : IInstanceIndexHolder
        {
            public static int instanceCount;

            public readonly int instanceIndex;

            public ImplWithInstanceIndex()
            {
                instanceCount++;
                instanceIndex = instanceCount;
            }

            public int GetInstanceIndex()
            {
                return instanceIndex;
            }

            public override string ToString()
            {
                return "ImplWithInstanceCounter " + instanceIndex;
            }
        }
    }
}
