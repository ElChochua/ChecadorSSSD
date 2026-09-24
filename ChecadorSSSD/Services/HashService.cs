using System;
using System.Security.Cryptography;
using System.Text;

namespace ChecadorSSSD.Services
{
    public static class HashService
    {
        public static string Hash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool Verify(string input, string hash)
        {
            var inputHash = Hash(input);
            return inputHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
