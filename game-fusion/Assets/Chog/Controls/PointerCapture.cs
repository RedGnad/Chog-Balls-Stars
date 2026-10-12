namespace Chog.Controls
{
    // Each joystick/button owns one finger until release or cancellation.
    public struct PointerCapture
    {
        private bool _captured;
        private int _pointerId;

        public bool TryCapture(int pointerId)
        {
            if (_captured) return false;
            _pointerId = pointerId;
            _captured = true;
            return true;
        }

        public bool Owns(int pointerId) => _captured && _pointerId == pointerId;

        public bool Release(int pointerId)
        {
            if (!Owns(pointerId)) return false;
            Reset();
            return true;
        }

        public void Reset() => _captured = false;
    }
}
