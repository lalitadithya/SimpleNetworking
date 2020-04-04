using NUnit.Framework;
using SimpleNetworking.SequenceGenerator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Tests.SequenceGenerator
{
    [TestFixture]
    public class ExponentialSequenceGeneratorTests
    {
        [Test]
        public void GeneratorSequenceMustBeCorrect()
        {
            ExponentialSequenceGenerator sequenceGenerator = new ExponentialSequenceGenerator(64);

            IEnumerator<int> enumerator = sequenceGenerator.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual(0, enumerator.Current);
            enumerator.MoveNext();
            Assert.AreEqual(2, enumerator.Current);
            enumerator.MoveNext();
            Assert.AreEqual(4, enumerator.Current);
            enumerator.MoveNext();
            Assert.AreEqual(8, enumerator.Current);
            enumerator.MoveNext();
            Assert.AreEqual(16, enumerator.Current);
            enumerator.MoveNext();
            Assert.AreEqual(32, enumerator.Current);
            enumerator.MoveNext();
            Assert.AreEqual(64, enumerator.Current);
            enumerator.MoveNext();
            Assert.AreEqual(64, enumerator.Current);
        }

    }
}
