using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.SequenceGenerator
{
    public abstract class SequenceGenerator : ISequenceGenerator
    {
        public abstract IEnumerator<int> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
