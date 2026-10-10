using NUnit.Framework;

// Flee's parser defaults follow the current culture: decimal separator, function argument
// separator and date literal format all come from CultureInfo.CurrentCulture (see
// ExpressionOptions). The tests are written for '.' as decimal separator, ',' between
// arguments and dd/MM/yyyy dates, so they would fail on a machine set to, for example,
// de-DE. Pinning the culture makes the results the same on every machine and CI runner.
// en-GB is used because en-CA, which the original Flee harness assumed, has used
// yyyy-MM-dd dates since .NET 5. Tests about culture handling set their own culture.
[assembly: SetCulture(Flee.Test.TestCulture.Name)]

namespace Flee.Test
{
    internal static class TestCulture
    {
        public const string Name = "en-GB";
    }
}
