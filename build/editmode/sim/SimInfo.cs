namespace Treeline.Sim
{
    /// <summary>
    /// Pure C# simulation. This assembly must not reference UnityEngine
    /// (enforced by noEngineReferences in Treeline.Sim.asmdef).
    /// </summary>
    public static class SimInfo
    {
        /// <summary>Bump when saved data or rules tables change shape.</summary>
        public const int RulesVersion = 1;
    }
}
