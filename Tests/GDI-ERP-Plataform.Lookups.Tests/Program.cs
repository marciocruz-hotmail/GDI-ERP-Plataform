using System;

namespace GdiPlataform.Lookups.Tests
{
    internal static class Program
    {
        public static int Main()
        {
            try
            {
                LookupCacheKeysTests.Run();
                LookupParametricCacheIntegrationTests.Run();
                LookupCacheInvalidationTests.Run();
                Console.WriteLine("OK: Lookup tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex.Message);
                Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}
