using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using UnityEngine;

public class RevealSystem : MonoBehaviour
{
    [SerializeField] private float _revealInterval = 7f;

    public event Action<Transform> OnEnemyPositionRevealed;

    private Coroutine _revealCoroutine;


    public void BeginReveals(Transform player, AIInputSource ai, float recordingDuration)
    {
        StopReveal();

        _revealCoroutine = StartCoroutine(RevealLoop(player, ai, recordingDuration));
    }
    public void StopReveal()
    {
        if (_revealCoroutine != null)
        {
            StopCoroutine(_revealCoroutine);
            _revealCoroutine = null;
        }
    }

    private IEnumerator RevealLoop(Transform player, AIInputSource ai, float recordingDuration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < recordingDuration)
        {
            if (player != null && ai != null)
            {
                Vector3 playerPosition = player.position;
                ai.ReceiveReveal(playerPosition);
                OnEnemyPositionRevealed?.Invoke(ai.transform);
            }
            yield return new WaitForSeconds(_revealInterval);
            elapsedTime += _revealInterval;
        }
    }
}