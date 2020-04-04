using NUnit.Framework;
using SimpleNetworking.IdempotencyService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SimpleNetworking.Tests.IdempotencyService
{
    [TestFixture]
    public class ReceiveIdempotencyService
    {
        [Test]
        public void ReceiveIdempotencyServiceShouldBeAbleToAdd()
        {
            IReceiveIdempotencyService<string> receiveIdempotencyService = new ReceiveIdempotencyService<string>(1 * 60000);

            Assert.IsTrue(receiveIdempotencyService.Add("Hello"));

            receiveIdempotencyService.Dispose();
        }

        [Test]
        public void ReceiveIdempotencyServiceShouldBeAbleToRemove()
        {
            IReceiveIdempotencyService<string> receiveIdempotencyService = new ReceiveIdempotencyService<string>(1 * 60000);

            receiveIdempotencyService.Add("Hello");
            Assert.IsTrue(receiveIdempotencyService.Remove("Hello"));

            receiveIdempotencyService.Dispose();
        }

        [Test]
        public void ReceiveIdempotencyServiceShouldRemoveItemsAfterTimeout()
        {
            IReceiveIdempotencyService<string> receiveIdempotencyService = new ReceiveIdempotencyService<string>(5 * 1000);

            receiveIdempotencyService.Add("Hello");

            Thread.Sleep(10 * 1000);

            Assert.IsFalse(receiveIdempotencyService.Remove("Hello"));

            receiveIdempotencyService.Dispose();
        }

        [Test]
        public void ReceiveIdempotencyServiceFindShouldReturnTrueIfValuePresent()
        {
            IReceiveIdempotencyService<string> receiveIdempotencyService = new ReceiveIdempotencyService<string>(1 * 60000);

            receiveIdempotencyService.Add("Hello");
            Assert.IsTrue(receiveIdempotencyService.Find("Hello"));

            receiveIdempotencyService.Dispose();
        }

        [Test]
        public void ReceiveIdempotencyServiceFindShouldReturnFalseIfValueNotPresent()
        {
            IReceiveIdempotencyService<string> receiveIdempotencyService = new ReceiveIdempotencyService<string>(1 * 60000);

            receiveIdempotencyService.Add("Hello");
            Assert.IsFalse(receiveIdempotencyService.Find("Hello1"));

            receiveIdempotencyService.Dispose();
        }

        [Test]
        public void ReceiveIdempotencyServiceFindShouldReturnFalseIfValueNotPresent1()
        {
            IReceiveIdempotencyService<string> receiveIdempotencyService = new ReceiveIdempotencyService<string>(1 * 60000);

            Assert.IsFalse(receiveIdempotencyService.Find("Hello"));

            receiveIdempotencyService.Dispose();
        }
    }
}
