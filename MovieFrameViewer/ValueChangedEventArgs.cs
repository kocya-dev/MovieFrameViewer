﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MovieFrameViewer
{
    public class ValueChangedEventArgs<T> : EventArgs
    {
        public T OldValue { get; private set; }
        public T NewValue { get; private set; }

        public ValueChangedEventArgs(T oldValue, T newValue){
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
