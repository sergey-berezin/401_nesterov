using System;
using System.Windows.Controls;


namespace WindowApp
{
    public class ProgressBarReporter : IProgress<double>
    {
        private readonly ProgressBar bar;

        public ProgressBarReporter(ref ProgressBar bar)
        {
            this.bar = bar;
        }

        public void Report(double value)
        {
            bar.Value = bar.Maximum * value;
        }

        public void Complete()
        {
            bar.Value = bar.Maximum;
        }

        public void Reset()
        {
            bar.Value = bar.Minimum;
        }
    }
}
