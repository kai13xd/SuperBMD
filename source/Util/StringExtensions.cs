using System.Runtime.CompilerServices;
public static class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this string str) => str.Length == 0;
}