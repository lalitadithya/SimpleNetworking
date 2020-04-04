using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.SequenceGenerator
{
    public class ExponentialSequenceGenerator : SequenceGenerator
    {
        private int numberOfFailedAttempts = 1;
        private readonly int maximumDelayInSeconds = 0;

        public ExponentialSequenceGenerator(int maximumDelayInSeconds)
        {
            this.maximumDelayInSeconds = maximumDelayInSeconds;
        }

        public override IEnumerator<int> GetEnumerator()
        {
            while (true)
            {
                var delayInSeconds = Math.Round(((1d / 2d) * (Math.Pow(2d, numberOfFailedAttempts++) - 1d)), 0);
                yield return maximumDelayInSeconds < delayInSeconds ? Convert.ToInt32(maximumDelayInSeconds) : Convert.ToInt32(delayInSeconds);
            }
        }
    }
}
