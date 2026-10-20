using System;
using System.Collections.Generic;
using System.Linq;

namespace AshAndDust.Utils
{

    public class WeightedParameter<T>
    {
        public T Item { get; }
        public double Ratio { get; }

        public WeightedParameter(T item, double ratio)
        {
            Item = item;
            Ratio = ratio;
        }
    }

    public class WeightedRandom<T>
    {
        public List<WeightedParameter<T>> Parameters;
        private Random r;

        public double RatioSum
        {
            get { return Parameters.Sum(p => p.Ratio); }
        }

        public WeightedRandom(params WeightedParameter<T>[] parameters)
        {
            Parameters = parameters.ToList();
            r = new Random();
        }

        public T GetRandom()
        {
            var numericValue = r.NextDouble() * RatioSum;

            foreach (var parameter in Parameters)
            {
                numericValue -= parameter.Ratio;

                if (numericValue > 0)
                    continue;

                return parameter.Item;
            }

            return Parameters[0].Item;
        }
    }
}