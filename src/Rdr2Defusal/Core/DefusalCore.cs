using System;

namespace Rdr2Defusal.Core
{
    public enum RoundState
    {
        Idle,
        Warmup,
        Live,
        PostRound
    }

    /// <summary>
    /// Minimal CS-style round loop:
    /// Idle -> Warmup -> Live -> PostRound -> Warmup...
    /// No bots yet; just timing + messages.
    /// </summary>
    public sealed class DefusalCore
    {
        public RoundState State { get; private set; } = RoundState.Idle;
        public event Action<RoundState> RoundStateChanged;

        private float _stateTime;
        private float _warmupLength = 6f;   // seconds
        private float _roundLength  = 120f; // seconds (2 min)
        private float _postLength   = 6f;   // seconds

        public bool IsRunning => State != RoundState.Idle;

        public void Start()
        {
            if (IsRunning) return;
            SetState(RoundState.Warmup);
        }

        public void Stop()
        {
            SetState(RoundState.Idle);
        }

        public void Tick(float dt)
        {
            if (!IsRunning) return;

            _stateTime += dt;

            switch (State)
            {
                case RoundState.Warmup:
                    if (_stateTime >= _warmupLength)
                        SetState(RoundState.Live);
                    break;

                case RoundState.Live:
                    if (_stateTime >= _roundLength)
                        SetState(RoundState.PostRound);
                    break;

                case RoundState.PostRound:
                    if (_stateTime >= _postLength)
                        SetState(RoundState.Warmup);
                    break;
            }
        }

        private void SetState(RoundState next)
        {
            State = next;
            _stateTime = 0f;
            RoundStateChanged?.Invoke(State);

            switch (State)
            {
                case RoundState.Idle:
                    break;

                case RoundState.Warmup:
                    break;

                case RoundState.Live:
                    break;

                case RoundState.PostRound:
                    break;
            }
        }

        private void DrawStateHUD()
        {
            // HUD handled by DefusalDevHarness overlay.
        }
    }
}
