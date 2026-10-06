using System;
using System.Collections.Generic;
using LittleDelay.Simulation.Core;

namespace LittleDelay.Simulation.Recording
{
    /// <summary>Captures one SimInputFrame per world tick - purely a list, nothing to drift.</summary>
    public sealed class SimInputRecorder
    {
        private readonly List<SimInputFrame> _frames = new();

        public int FrameCount => _frames.Count;

        public void Clear() => _frames.Clear();

        public void RecordFrame(SimInputFrame frame) => _frames.Add(frame);

        public SimInputRecording ToRecording() => new SimInputRecording(_frames.ToArray());
    }
}
