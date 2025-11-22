using System;
using RDR2.UI;
using RScreen = RDR2.UI.Screen;

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

        private float _stateTime;
        private float _warmupLength = 6f;   // seconds
        private float _roundLength  = 120f; // seconds (2 min)
        private float _postLength   = 6f;   // seconds

        private float _hudAccumulator;

        public bool IsRunning => State != RoundState.Idle;

        public void Start()
        {
            if (IsRunning) return;
            SetState(RoundState.Warmup);
        }

        public void Stop()
        {
            SetState(RoundState.Idle);
            RScreen.DisplaySubtitle("[Defusal] Stopped.");
        }

        public void Tick(float dt)
        {
            if (!IsRunning) return;

            _stateTime += dt;
            _hudAccumulator += dt;

            // lightweight HUD ping every 0.5s
            if (_hudAccumulator >= 0.5f)
            {
                _hudAccumulator = 0f;
                DrawStateHUD();
            }

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

            switch (State)
            {
                case RoundState.Idle:
                    break;

                case RoundState.Warmup:
                    RScreen.DisplaySubtitle("[Defusal] Warmup… round starting.");
                    break;

                case RoundState.Live:
                    RScreen.DisplaySubtitle("[Defusal] LIVE. Fight for sites.");
                    break;

                case RoundState.PostRound:
                    RScreen.DisplaySubtitle("[Defusal] Round over. Resetting…");
                    break;
            }
        }

        private void DrawStateHUD()
        {
            float remain = 0f;
            if (State == RoundState.Warmup) remain = Math.Max(0f, _warmupLength - _stateTime);
            if (State == RoundState.Live)   remain = Math.Max(0f, _roundLength  - _stateTime);
            if (State == RoundState.PostRound) remain = Math.Max(0f, _postLength - _stateTime);

            RScreen.DisplaySubtitle($"[Defusal] {State} | {remain:0.0}s");
        }
    }
}
