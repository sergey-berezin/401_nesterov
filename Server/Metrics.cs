namespace Server
{
    public class PairVectors : Tuple<float[], float[]>
    {
        public PairVectors(float[] item1, float[] item2) : base(item1, item2)
        {
        }

        public bool NotNull()
        {
            return Item1 != null && Item2 != null;
        }
    }

    public interface IMetric
    {
        public string Name { get; }
        public float? Value { get; }
        public IMetric Compute(PairVectors pair);
        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                { Name, Value == null ? "<Undefined>" : Value },
            };
        }
    }

    public class Distance : IMetric
    {
        public string Name => "Distance";
        private float? _value = null;
        public float? Value { get => _value; }

        public static float Length(float[] v) =>
        (float)Math.Sqrt(v.Select(x => x * x).Sum());

        public IMetric Compute(PairVectors pair)
        {
            if (pair.NotNull())
                _value = Length(pair.Item1.Zip(pair.Item2).Select(
                    p => p.First - p.Second
                    ).ToArray());
            return this;
        }
    }

    public class Similarity : IMetric
    {
        public string Name => "Similarity";
        private float? _value = null;
        public float? Value { get => _value; }
        public IMetric Compute(PairVectors pair)
        {
            if (pair.NotNull())
                _value = pair.Item1.Zip(pair.Item2).Select(
                    p => p.First * p.Second
                ).Sum();
            return this; 
        }
    }
}
