using System;
using System.Security.Cryptography;
using System.Text;

namespace BuyBidBlow.Systems.Account
{
    // PBKDF2-HMAC-SHA256. 저장 형식: pbkdf2_sha256$반복수$salt(Base64)$hash(Base64)
    // 반복수를 문자열에 함께 저장하므로 나중에 값을 올려도 기존 계정은 그대로 검증된다.
    // Mono의 Rfc2898DeriveBytes 지원 차이를 피하려고 HMACSHA256으로 직접 계산한다.
    public static class PasswordHasher
    {
        const string Scheme = "pbkdf2_sha256";
        public const int Iterations = 100000;
        const int SaltBytes = 16, HashBytes = 32;

        public static string Hash(string password)
        {
            var salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            var hash = Derive(password, salt, Iterations);
            return $"{Scheme}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string stored)
        {
            var parts = (stored ?? "").Split('$');
            if (parts.Length != 4 || parts[0] != Scheme || !int.TryParse(parts[1], out int iterations) || iterations <= 0) return false;
            byte[] salt, expected;
            try { salt = Convert.FromBase64String(parts[2]); expected = Convert.FromBase64String(parts[3]); }
            catch (FormatException) { return false; }
            if (expected.Length != HashBytes) return false;
            var actual = Derive(password, salt, iterations);
            int diff = 0;
            for (int i = 0; i < HashBytes; i++) diff |= actual[i] ^ expected[i];
            return diff == 0;
        }

        // HashBytes(32) == SHA256 출력 길이라 블록 하나만 계산한다.
        static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(password ?? "")))
            {
                var block = new byte[salt.Length + 4];
                Buffer.BlockCopy(salt, 0, block, 0, salt.Length);
                block[block.Length - 1] = 1;
                var u = hmac.ComputeHash(block);
                var result = (byte[])u.Clone();
                for (int i = 1; i < iterations; i++)
                {
                    u = hmac.ComputeHash(u);
                    for (int j = 0; j < result.Length; j++) result[j] ^= u[j];
                }
                return result;
            }
        }
    }
}
