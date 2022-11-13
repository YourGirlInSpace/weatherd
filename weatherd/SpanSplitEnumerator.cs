using System;
using System.Linq;

namespace weatherd
{
    public ref struct SpanSplitEnumerator
    {
        public ReadOnlySpan<char> Current { get; private set; }
        public ReadOnlySpan<char> Original => _originalSpan;
        public int Index => _currentIndex;

        private ReadOnlySpan<char> _originalSpan;
        private readonly char[] _splitChars;
        private readonly StringSplitOptions _splitOptions;
        private int _currentIndex;
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
            _currentIndex = 0;
            _lastStart = 0;
            Current = default;
        }

        public bool MoveNext()
        {
            if (_currentIndex >= _originalSpan.Length)
                return false;

            bool isEmpty = false;
            do
            {
                if (_currentIndex >= _originalSpan.Length)
                    break;

                ReadOnlySpan<char> substr = default;
                int startIndex = _currentIndex;
                for (; _currentIndex < _originalSpan.Length; _currentIndex++)
                {
                    if (!_splitChars.Contains(_originalSpan[_currentIndex]))
                        continue;
                    
                    substr = _originalSpan[startIndex.._currentIndex];

                    if (substr.Length == 0 && _splitOptions.HasFlag(StringSplitOptions.RemoveEmptyEntries))
                    {
                        isEmpty = true;
                        break;
                    }
                    
                    isEmpty = false;
                    break;
                }

                if (_currentIndex == _originalSpan.Length)
                {
                    substr = _originalSpan[startIndex.._currentIndex];
                    isEmpty = false;
                }
                
                _currentIndex++;
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
            _currentIndex = _lastStart;
        }

        public void Reset()
        {
            _currentIndex = 0;
        }
    }
}
