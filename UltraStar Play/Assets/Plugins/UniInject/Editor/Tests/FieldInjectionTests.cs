using NUnit.Framework;
using UniInject;

// Disable warning about never assigned fields. The values are injected.
#pragma warning disable CS0649

namespace UniInject.Tests
{

    public class FieldInjectionTests
    {
        [Test]
        public void FieldInjectionFromExistingInstance()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.BindExistingInstance("abc");

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsFieldInjection needsInjection = new NeedsFieldInjection();
            injector.Inject(needsInjection);
            Assert.AreEqual("abc", needsInjection.theString);
        }

        [Test]
        public void FieldInjectionFromSingleInstanceOfType()
        {
            ImplWithInstanceIndex.instanceCount = 0;

            BindingBuilder bb = new BindingBuilder();
            bb.BindTypeToSingleInstance(typeof(ImplWithInstanceIndex));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsFieldInjection needsInjection1 = injector.CreateAndInject<NeedsFieldInjection>();
            NeedsFieldInjection needsInjection2 = injector.CreateAndInject<NeedsFieldInjection>();
            NeedsFieldInjection needsInjection3 = injector.CreateAndInject<NeedsFieldInjection>();

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
        public void FieldInjectionFromNewInstancesOfType()
        {
            ImplWithInstanceIndex.instanceCount = 0;

            BindingBuilder bb = new BindingBuilder();
            bb.BindTypeToNewInstances(typeof(ImplWithInstanceIndex));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsFieldInjection needsInjection1 = injector.CreateAndInject<NeedsFieldInjection>();
            NeedsFieldInjection needsInjection2 = injector.CreateAndInject<NeedsFieldInjection>();
            NeedsFieldInjection needsInjection3 = injector.CreateAndInject<NeedsFieldInjection>();

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
        public void NonOptionalFieldInjectionThrowsExceptionIfNotPossible()
        {
            Injector injector = UniInjectUtils.CreateInjector();

            NeedsNonOptionalFieldInjectionNonOptional needsInjection = new NeedsNonOptionalFieldInjectionNonOptional();

            Assert.Throws<InjectionException>(delegate { injector.Inject(needsInjection); });
        }

        [Test]
        public void FieldInjectionOfInterfaceThatIsBoundToAnImplementation()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.Bind(typeof(IInstanceIndexHolder)).ToSingleInstanceOfType(typeof(ImplWithInstanceIndex));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsFieldInjectionOfInterface needsInjection = injector.CreateAndInject<NeedsFieldInjectionOfInterface>();

            Assert.NotNull(needsInjection.interfaceInstance);
        }

        [Test]
        public void FieldInjectionWithCustomKey()
        {
            BindingBuilder bb = new BindingBuilder();
            bb.Bind("author").ToExistingInstance("Tolkien");

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);

            NeedsFieldInjectionWithCustomKey needsInjection = injector.CreateAndInject<NeedsFieldInjectionWithCustomKey>();

            Assert.AreEqual("Tolkien", needsInjection.theAuthor);
        }

        [Test]
        public void CyclicFieldInjection()
        {
            NeedsFieldInjectionCyclic_A.instanceCount = 0;
            NeedsFieldInjectionCyclic_B.instanceCount = 0;
            NeedsFieldInjectionCyclic_C.instanceCount = 0;

            BindingBuilder bb = new BindingBuilder();
            bb.BindTypeToNewInstances(typeof(NeedsFieldInjectionCyclic_A));
            bb.BindTypeToNewInstances(typeof(NeedsFieldInjectionCyclic_B));
            bb.BindTypeToNewInstances(typeof(NeedsFieldInjectionCyclic_C));

            Injector injector = UniInjectUtils.CreateInjector();
            injector.AddBindings(bb);
            NeedsFieldInjectionCyclic_A aInstance = injector.CreateAndInject<NeedsFieldInjectionCyclic_A>();

            // The dependency in C must have been resolved with the instance of A that was created.
            Assert.NotNull(aInstance.bInstance);
            Assert.NotNull(aInstance.bInstance.cInstance);
            Assert.NotNull(aInstance.bInstance.cInstance.aInstance);
            Assert.AreEqual(aInstance.instanceIndex, aInstance.bInstance.cInstance.aInstance.instanceIndex);
        }

        private class NeedsFieldInjectionWithCustomKey
        {
            [Inject(key = "author")]
            public string theAuthor;
        }

        private class NeedsFieldInjectionCyclic_A
        {
            public static int instanceCount;
            public readonly int instanceIndex;

            [Inject]
            public NeedsFieldInjectionCyclic_B bInstance;

            public NeedsFieldInjectionCyclic_A()
            {
                instanceCount++;
                instanceIndex = instanceCount;
            }
        }

        private class NeedsFieldInjectionCyclic_B
        {
            public static int instanceCount;
            public readonly int instanceIndex;

            [Inject]
            public NeedsFieldInjectionCyclic_C cInstance;

            public NeedsFieldInjectionCyclic_B()
            {
                instanceCount++;
                instanceIndex = instanceCount;
            }
        }

        private class NeedsFieldInjectionCyclic_C
        {
            public static int instanceCount;
            public readonly int instanceIndex;

            [Inject]
            public NeedsFieldInjectionCyclic_A aInstance;

            public NeedsFieldInjectionCyclic_C()
            {
                instanceCount++;
                instanceIndex = instanceCount;
            }
        }

        private class NeedsNonOptionalFieldInjectionNonOptional
        {
            [Inject]
            public string theString;
        }

        private class NeedsFieldInjectionOfInterface
        {
            [Inject]
            public IInstanceIndexHolder interfaceInstance;
        }

        private class NeedsFieldInjection
        {
            [Inject(optional = true)]
            public string theString;

            [Inject(optional = true)]
            public ImplWithInstanceIndex implWithInstanceCounter;
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
