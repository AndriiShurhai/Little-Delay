using System;
using LittleDelay.Simulation.Abilities;

namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// Identifies the weapon and up to two active abilities equipped by a fighter.
    /// </summary>
    [Serializable]
    public readonly struct SimLoadout
    {
        public readonly string WeaponId;
        public readonly string Ability1Id;
        public readonly string Ability2Id;

        public SimLoadout(string weaponId, string ability1Id, string ability2Id = "")
        {
            WeaponId = weaponId ?? string.Empty;
            Ability1Id = ability1Id ?? string.Empty;
            Ability2Id = ability2Id ?? string.Empty;
        }

        public static SimLoadout Default => new SimLoadout(SimBlastAbility.Id, SimDashAbility.Id, string.Empty);
    }
}