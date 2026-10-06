using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Flow;
using Unity.Cinemachine;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// The camera rig behind ICameraDirector. Same mechanism the old BattleManager used: the player
    /// camera is switched on for the third-person view and off to fall back to the arena camera, and the
    /// CinemachineBrain does the blend. The match flow only asks for a view and waits on IsBlending.
    /// </summary>
    public sealed class CameraDirector : MonoBehaviour, ICameraDirector
    {
        [SerializeField] private GameObject _arenaCamera;
        [SerializeField] private GameObject _playerCamera;
        [Tooltip("The CinemachineBrain on the Main Camera.")]
        [SerializeField] private CinemachineBrain _cinemachineBrain;

        public bool IsBlending => _cinemachineBrain != null && _cinemachineBrain.IsBlending;

        public void ShowView(CameraView view)
        {
            if (_arenaCamera != null) _arenaCamera.SetActive(true);
            if (_playerCamera != null) _playerCamera.SetActive(view == CameraView.Player);
        }
    }
}
