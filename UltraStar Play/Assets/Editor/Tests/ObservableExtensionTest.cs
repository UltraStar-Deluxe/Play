using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UniRx;

public class ObservableExtensionTest
{
    public class FirstOrDefaultAsyncTest
    {
        [Test]
        public async Task ShouldReturnFirstValue()
        {
            IObservable<int> observableWithValues = Observable.Range(1, 5);
            int result = await observableWithValues.FirstOrDefaultAsync();
            Assert.AreEqual(1, result);
        }

        [Test]
        public async Task ShouldReturnDefaultValue()
        {
            IObservable<int> emptyObservable = Observable.Empty<int>();
            int result = await emptyObservable.FirstOrDefaultAsync();
            Assert.AreEqual(0, result);
        }
    }

    public class FirstAsyncTest
    {
        [Test]
        public async Task ShouldReturnFirstValue()
        {
            IObservable<int> observable = Observable.Range(1, 5);
            int result = await observable.FirstAsync();
            Assert.AreEqual(1, result);
        }

        [Test]
        public async Task ShouldThrowException()
        {
            IObservable<int> emptyObservable = Observable.Empty<int>();
            Assert.ThrowsAsync<InvalidOperationException>(async () => await emptyObservable.FirstAsync());
        }
    }
}
