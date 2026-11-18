namespace TwoLadles.Core
{
    /// <summary>
    /// Tiny typed-sequence matcher (pure logic, no allocations per key).
    /// Feed one character at a time; returns true on the key that completes the code, then resets.
    /// Case-insensitive. A wrong key resets, but if that key is the code's first letter matching restarts from it.
    /// </summary>
    public class CheatCode
    {
        private readonly string code;
        private int index;

        public CheatCode(string code)
        {
            this.code = string.IsNullOrEmpty(code) ? "" : code.ToUpperInvariant();
        }

        public bool Feed(char c)
        {
            if (code.Length == 0) return false;
            c = char.ToUpperInvariant(c);
            if (c == code[index]) index++;
            else index = c == code[0] ? 1 : 0;

            if (index >= code.Length)
            {
                index = 0;
                return true;
            }
            return false;
        }
    }
}
