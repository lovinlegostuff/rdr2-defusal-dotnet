using System;
using System.Collections.Generic;

namespace Rdr2Defusal.UI
{
    /// <summary>
    /// Lightweight notification queue rendered with our native draw text.
    /// Avoids ScriptHook subtitles to prevent audio chirps.
    /// </summary>
    public sealed class NotificationManager
    {
        private readonly Queue<Notification> _queue = new Queue<Notification>();
        private Notification _active;

        public void Enqueue(string message, float duration = 3f)
        {
            _queue.Enqueue(new Notification { Message = message, Duration = duration });
        }

        public void Tick(float dt)
        {
            if (_active == null && _queue.Count > 0)
                _active = _queue.Dequeue();

            if (_active == null) return;

            _active.Time += dt;
            if (_active.Time >= _active.Duration)
                _active = null;
        }

        public void Draw()
        {
            if (_active == null) return;
            float x = 0.5f;
            float y = 0.05f;
            NativeDraw.Rect(x, y + 0.012f, 0.32f, 0.040f, 0, 0, 0, 160);
            NativeDraw.TextWrapped(_active.Message, x - 0.15f, y, 0.38f, 0.38f, 255, 255, 255, 255, 0.30f);
        }
    }

    public sealed class Notification
    {
        public string Message;
        public float Duration;
        public float Time;
    }
}
