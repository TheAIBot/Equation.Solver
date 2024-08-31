namespace Equation.Solver;

internal struct FastResetBoolArray
{
    // First value representing true can not be 0 since the definition
    // of false is defined as _isTrueValue - 1. This would make 255 the
    // false value. This becomes a problem because the true value will
    // later become 255 which would then reinterpret old false values
    // as true. To solve this the first _isTrueValue is defined as 1
    // which ensures the false value is always less than the true value.
    private const byte _firstTrueValue = 1;
    private readonly byte[] _values;
    private byte _isTrueValue;

    public int Length => _values.Length;

    public int TrueCount => CountTrueValues();

    public bool this[int index]
    {
        get => _values[index] == _isTrueValue;
        set { _values[index] = value ? _isTrueValue : (byte)(_isTrueValue - 1); }
    }

    public FastResetBoolArray(int length)
    {
        _values = new byte[length];
        _isTrueValue = _firstTrueValue;
    }

    public void SetRangeTrue(int startIndex, int count, bool value)
    {
        byte booleanValue = value ? _isTrueValue : (byte)(_isTrueValue - 1);
        Array.Fill(_values, booleanValue, startIndex, count);
    }

    public void CopyTo(ref FastResetBoolArray otherArray)
    {
        Array.Copy(_values, otherArray._values, _values.Length);
        otherArray._isTrueValue = _isTrueValue;
    }

    public void Clear()
    {
        // Clearing is efficient because we can just change the
        // definition of true. This works in all cases except
        // when the byte overflows. In that case we can not
        // just start from _firstTrueValue because old values may still
        // be present in the array. To remove all old values
        // the array is cleared. This reduces the cost of
        // clearing the array to ~1/255 of its previous cost.
        if (_isTrueValue == byte.MaxValue)
        {
            Array.Clear(_values);
            _isTrueValue = _firstTrueValue;
            return;
        }

        _isTrueValue++;
    }

    private int CountTrueValues()
    {
        int trueCount = 0;
        for (int i = 0; i < _values.Length; i++)
        {
            trueCount += _values[i] == _isTrueValue ? 1 : 0;
        }

        return trueCount;
    }
}
