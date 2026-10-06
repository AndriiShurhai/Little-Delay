namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// Recording: the rehearsal world both fighters act in live while inputs are
    /// captured - projectiles fly and bounce but nothing takes damage, nobody can
    /// die, the arena doesn't shrink, reveal windows are active.
    /// Battle: a fresh world where both recordings replay - damage, passivity,
    /// arena shrink and round resolution are live.
    /// </summary>
    public enum SimWorldMode
    {
        Recording,
        Battle
    }
}
