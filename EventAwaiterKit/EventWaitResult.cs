using System;

namespace EventAwaiterKit
{
    /// <summary>Distinguishes an event payload, including null or default, from timeout.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    public readonly struct EventWaitResult<T>
    {
        private readonly T value;

        internal EventWaitResult(T value)
        {
            this.value = value;
            Occurred = true;
        }

        /// <summary>Gets whether the event occurred. False represents timeout.</summary>
        public bool Occurred { get; }

        /// <summary>Gets the event payload, which can itself be null or default.</summary>
        /// <exception cref="InvalidOperationException">The wait timed out.</exception>
        public T Value => Occurred ? value : throw new InvalidOperationException("The wait timed out; there is no event payload.");
    }
}
