using System;
using System.Collections.Generic;
using LittleDelay.Simulation.Core;

namespace LittleDelay.Simulation.Recording
{
    /// <summary>
    /// An immutable, tick-indexed input stream: frame N is what the fighter did on world tick N.
    /// Because it holds no positions, replaying it through a SimWorld started from the same spawn
    /// reproduces the recorded run exactly.
    /// </summary>
    public sealed class SimInputRecording
    {
        private readonly SimInputFrame[] _frames;

        public int FrameCount => _frames.Length;

        internal SimInputRecording(SimInputFrame[] frames)
        {
            _frames = frames;
        }

        public static SimInputRecording Empty { get; } = new SimInputRecording(Array.Empty<SimInputFrame>());

        /// <summary>Past the end of the stream the fighter does nothing.</summary>
        public SimInputFrame GetFrame(int tickIndex)
        {
            return tickIndex >= 0 && tickIndex < _frames.Length ? _frames[tickIndex] : SimInputFrame.None;
        }
    }
}
