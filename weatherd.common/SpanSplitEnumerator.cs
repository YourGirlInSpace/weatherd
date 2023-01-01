using System;
using System.Linq;

namespace weatherd
{
    public ref struct SpanSplitEnumerator
    {
        public ReadOnlySpan<char> Current { get; private set; }
        public ReadOnlySpan<char> Original => _originalSpan;
        public int Index { get; private set; }

        private readonly ReadOnlySpan<char> _originalSpan;
        private readonly char[] _splitChars;
        private readonly StringSplitOptions _splitOptions;
        private int _lastStart;

        public SpanSplitEnumerator(ReadOnlySpan<char> span, params char[] splitChars)
            : this(span, splitChars, StringSplitOptions.None)
        { }

        public SpanSplitEnumerator(ReadOnlySpan<char> span, char splitChar, StringSplitOptions options)
            : this(span, new [] { splitChar }, options)
        { }

        public SpanSplitEnumerator(ReadOnlySpan<char> span, char[] splitChars, StringSplitOptions options)
        {
            _originalSpan = span;
            _splitChars = splitChars;
            _splitOptions = options;
            Index = 0;
            _lastStart = 0;
            Current = default;
        }

        public bool MoveNext()
        {
            if (Index >= _originalSpan.Length)
                return false;

            bool isEmpty = false;
            do
            {
                if (Index >= _originalSpan.Length)
                    break;

                ReadOnlySpan<char> substr = default;
                int startIndex = Index;
                for (; Index < _originalSpan.Length; Index++)
                {
                    if (!_splitChars.Contains(_originalSpan[Index]))
                        continue;
                    
                    substr = _originalSpan[startIndex..Index];

                    if (substr.Length == 0 && _splitOptions.HasFlag(StringSplitOptions.RemoveEmptyEntries))
                    {
                        isEmpty = true;
                        break;
                    }
                    
                    isEmpty = false;
                    break;
                }

                if (Index == _originalSpan.Length)
                {
                    substr = _originalSpan[startIndex..Index];
                    isEmpty = false;
                }
                
                Index++;
                if (isEmpty)
                    continue;

                _lastStart = startIndex;
                Current = substr;
                return true;
            } while (true);

            return false;
        }

        public void BackOne()
        {
            Index = _lastStart;
        }

        public void Reset()
        {
            Index = 0;
        }
    }
}
