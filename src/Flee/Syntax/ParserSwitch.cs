#nullable enable

namespace Flee.Syntax
{
    /// <summary>
    /// Investigation switch: <c>FLEE_PARSER=parlot</c> in the environment makes every context use the
    /// Parlot grammar instead of the Grammatica parser. Read once, at first use.
    /// </summary>
    internal static class ParserSwitch
    {
        public static readonly bool UseParlot =
            string.Equals(Environment.GetEnvironmentVariable("FLEE_PARSER"), "parlot", StringComparison.OrdinalIgnoreCase);
    }
}
