using System;
using System.Collections.Generic;

namespace GdiPlataform.Lookups.Tests
{
    internal static class LookupTestAssert
    {
        public static void AreNotEqual<T>(T expectedDifferentA, T expectedDifferentB, string message = null)
        {
            if (EqualityComparer<T>.Default.Equals(expectedDifferentA, expectedDifferentB))
                throw new InvalidOperationException(message ?? "Valores deveriam ser diferentes.");
        }

        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
                throw new InvalidOperationException(message ?? "Condicao falsa.");
        }

        public static void SameReference<T>(T a, T b, string message = null) where T : class
        {
            if (!ReferenceEquals(a, b))
                throw new InvalidOperationException(message ?? "Referencia deveria ser a mesma (cache hit).");
        }
    }
}
