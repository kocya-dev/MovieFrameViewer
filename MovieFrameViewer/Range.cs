using System;
using System.Collections.Generic;
using System.Text;

namespace MovieFrameViewer
{
    public class Range<T>
    {
        public T Min { get; private set; }
        public T Max { get; private set; }
        public T Value { get; private set; }

        public Range(T min, T max, T value)
        {
            Min = min;
            Max = max;
            Value = value;
        }
        public Range(T min, T max)
            : this(min, max, min)
        {
        }
    }
}
