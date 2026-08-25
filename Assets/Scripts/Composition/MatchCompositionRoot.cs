using Drewio.UnityToolkit.Core;
using Drewio.UnityToolkit.Timers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LittleDelay.Composition
{
    /// <summary>
    /// Scene-scoped composition root for the arena scene. Unlike
    /// GameCompositionRoot, this does NOT survive scene reloads - a fresh
    /// instance is created every time the arena scene loads, whether that's the
    /// very first match of a session or "play again" after GameOver, so nothing
    /// match-scoped (a projectile pool, the match StateMachine, live SimFighter
    /// instances) ever leaks state between matches by surviving one it shouldn't.
    /// 
    /// Resolves what it needs from GameCompositionRoot.Services exactly once,
    /// here, and passes it into match-scoped objects via their constructors -
    /// later phases build on this Awake rather than reaching for 
    /// GameCompositionRoot.Instance from scattered MonoBehaviours elsewhere in the scene
    /// </summary>
    public class MatchCompositionRoot : MonoBehaviour
    {
        public IEventBus EventBus { get; private set; }
        public TimerManager TimerManager { get; private set; }

        private void Awake()
        {
            var rootServices = GameCompositionRoot.Instance.Services;
            EventBus = rootServices.Resolve<IEventBus>();
            TimerManager = rootServices.Resolve<TimerManager>();

            
        }
    }
}