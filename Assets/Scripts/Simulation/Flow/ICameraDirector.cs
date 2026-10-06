using LittleDelay.Simulation.Events;

namespace LittleDelay.Simulation.Flow
{
    /// <summary>The one thing the match flow needs from the camera rig: switch view, and know when the blend is over.</summary>
    public interface ICameraDirector
    {
        bool IsBlending { get; }
        void ShowView(CameraView view);
    }

    /// <summary>No-op director for tests and for scenes without a camera rig.</summary>
    public sealed class NullCameraDirector : ICameraDirector
    {
        public bool IsBlending => false;
        public void ShowView(CameraView view) { }
    }
}
