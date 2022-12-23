namespace Server
{
    public class MetricsList : List<IMetric>
    {
        public MetricsList(IEnumerable<IMetric> collection) : base(collection)
        {
        }

        public MetricsList Compute(PairVectors pair_embeddings)
        {
            return new MetricsList(this.Select(x => x.Compute(pair_embeddings)).ToList());
        }

        public Dictionary<string, float?> ToValues()
        {
            return this.Select(x => x.ToValues())
            .SelectMany(dict => dict)
            .ToDictionary(g => g.Key, g => g.Value);
        }
    }
}
