namespace Equation.Solver;

internal sealed class SampleAverage
{
    private readonly int _sampleCount;
    private readonly Queue<float> _samples = new Queue<float>();

    public SampleAverage(int sampleCount)
    {
        _sampleCount = sampleCount;
    }

    public void AddSample(float sample)
    {
        _samples.Enqueue(sample);
        if (_samples.Count == _sampleCount + 1)
        {
            _samples.Dequeue();
        }
    }

    public float GetAverage()
    {
        return _samples.Average();
    }
}
