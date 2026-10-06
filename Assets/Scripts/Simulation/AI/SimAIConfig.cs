using System;

namespace LittleDelay.Simulation.AI
{
    /// <summary>Tuning for the recording-phase bot. Plain serializable class so tests can build one without Unity assets.</summary>
    [Serializable]
    public class SimAIConfig
    {
        public float EngageDistance = 9f;
        public float RetreatDistance = 4f;
        public float FireRange = 14f;
        public float FireConeDegrees = 12f;
        public float StrafeSwitchSeconds = 1.2f;
        [UnityEngine.Range(0f, 1f)] public float StrafeStrength = 0.8f;
        [UnityEngine.Tooltip("How far past the last reveal the bot will extrapolate the player's velocity.")]
        public float PredictionHorizonSeconds = 1.5f;
        public float DashIntervalSeconds = 3f;
    }
}
