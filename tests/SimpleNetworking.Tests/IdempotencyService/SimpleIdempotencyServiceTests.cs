using NUnit.Framework;
using SimpleNetworking.IdempotencyService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Tests.IdempotencyService
{
    [TestFixture]
    public class SimpleIdempotencyServiceTests
    {
        public class TestClass
        {
            public int Value { get; set; }

            public TestClass(int value)
            {
                Value = value;
            }
        }


        [Test]
        public async Task IdempotencyServiceShouldBeAbleToAddData()
        {
            ISimpleIdempotencyService<int, TestClass> simpleIdempotencyService = new SimpleIdempotencyService<int, TestClass>(10);
            TestClass testClass = new TestClass(42);
            bool result = await simpleIdempotencyService.Add(1, testClass);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task IdempotencyServiceShouldBlockAfterPacketLimitExceeds()
        {
            ISimpleIdempotencyService<int, TestClass> simpleIdempotencyService = new SimpleIdempotencyService<int, TestClass>(2);
            await simpleIdempotencyService.Add(1, new TestClass(42));
            await simpleIdempotencyService.Add(2, new TestClass(43));
            bool taskCompleted = simpleIdempotencyService.Add(3, new TestClass(44)).Wait(2 * 1000);
            Assert.IsTrue(!taskCompleted);
        }

        [Test]
        public async Task IdempotencyServiceShouldRemoveValueBasedOnKey()
        {
            ISimpleIdempotencyService<int, TestClass> simpleIdempotencyService = new SimpleIdempotencyService<int, TestClass>(2);
            await simpleIdempotencyService.Add(1, new TestClass(42));
            Assert.IsTrue(simpleIdempotencyService.Remove(1, out TestClass value));
            Assert.AreEqual(42, value.Value);
        }

        [Test]
        public async Task IdempotencyServiceShouldUnblockAfterPacketLimitIsNoLongerExceeded()
        {
            ISimpleIdempotencyService<int, TestClass> simpleIdempotencyService = new SimpleIdempotencyService<int, TestClass>(2);
            await simpleIdempotencyService.Add(1, new TestClass(42));
            await simpleIdempotencyService.Add(2, new TestClass(43));
            Task<bool> thirdAdd = Task.Run(() => simpleIdempotencyService.Add(3, new TestClass(44)).Wait(5 * 1000));
            simpleIdempotencyService.Remove(1, out _);
            Assert.IsTrue(await thirdAdd);
        }

        [Test]
        public async Task IdempotencyServiceShouldEnumurateValues()
        {
            ISimpleIdempotencyService<int, TestClass> simpleIdempotencyService = new SimpleIdempotencyService<int, TestClass>(2);
            await simpleIdempotencyService.Add(1, new TestClass(42));
            await simpleIdempotencyService.Add(2, new TestClass(43));

            List<TestClass> values = simpleIdempotencyService.GetValues().ToList();

            Assert.AreEqual(2, values.Count);
            Assert.IsTrue(values.Any(x => x.Value == 42));
            Assert.IsTrue(values.Any(x => x.Value == 43));
        }

        [Test]
        public async Task IdempotencyServiceShouldNotEnumurateValuesThatAreRemoved()
        {
            ISimpleIdempotencyService<int, TestClass> simpleIdempotencyService = new SimpleIdempotencyService<int, TestClass>(2);
            await simpleIdempotencyService.Add(1, new TestClass(42));
            await simpleIdempotencyService.Add(2, new TestClass(43));

            List<TestClass> values = new List<TestClass>();
            bool firstIteration = true;
            foreach (var item in simpleIdempotencyService.GetValues())
            {
                values.Add(item);
                if(firstIteration)
                {
                    firstIteration = false;
                    simpleIdempotencyService.Remove(2, out _);
                }
            }

            Assert.AreEqual(1, values.Count);
            Assert.IsTrue(values.Any(x => x.Value == 42));
            Assert.IsFalse(values.Any(x => x.Value == 43));
        }

        [Test]
        public async Task IdempotencyServiceShouldNotEnumurateValuesThatAreRemoved1()
        {
            ISimpleIdempotencyService<int, TestClass> simpleIdempotencyService = new SimpleIdempotencyService<int, TestClass>(3);
            await simpleIdempotencyService.Add(1, new TestClass(42));
            await simpleIdempotencyService.Add(2, new TestClass(43));
            await simpleIdempotencyService.Add(3, new TestClass(44));

            List<TestClass> values = new List<TestClass>();
            bool firstIteration = true;
            foreach (var item in simpleIdempotencyService.GetValues())
            {
                values.Add(item);
                if (firstIteration)
                {
                    firstIteration = false;
                    simpleIdempotencyService.Remove(2, out _);
                }
            }

            Assert.AreEqual(2, values.Count);
            Assert.IsTrue(values.Any(x => x.Value == 42));
            Assert.IsTrue(values.Any(x => x.Value == 44));
            Assert.IsFalse(values.Any(x => x.Value == 43));
        }
    }
}
