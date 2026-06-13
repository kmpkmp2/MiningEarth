using System;

namespace DeepEarth.Map
{
    public class DepthData
    {
        private int _currentDepth;
        public int CurrentDepth
        {
            get => _currentDepth;
            set
            {
                if (_currentDepth != value)
                {
                    _currentDepth = value;
                    OnDepthChanged?.Invoke(_currentDepth);
                }
            }
        }

        public event Action<int> OnDepthChanged;

        public DepthData(int initialDepth = 0)
        {
            _currentDepth = initialDepth;
        }
    }
}
