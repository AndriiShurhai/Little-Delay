using System;
using System.Collections.Generic;
using LittleDelay.Simulation.Core;

namespace LittleDelay.Simulation.Recording
{
    /// <summary>Plays a recording back as an ISimInputSource; past the end the fighter does nothing.</summary>
    public sealed class SimRecordedInputSource : ISimInputSource
    {
        private readonly SimInputRecording _recording;

        public SimRecordedInputSource(SimInputRecording recording)
        {
            _recording = recording ?? throw new ArgumentNullException(nameof(recording));
        }

        public bool IsExhausted(int tickIndex) => tickIndex >= _recording.FrameCount;

        public SimInputFrame GetInput(int tickIndex) => _recording.GetFrame(tickIndex);
    }
}
