using System.Security.Cryptography;

namespace FrierenHR.Application.Common.Security;

/// <summary>
/// Generates the password HR hands to somebody for their first sign-in.
///
/// It only has to survive being read aloud or typed once, because the account is forced to
/// change it immediately. So the alphabet leaves out the characters people misread, and the
/// result is still drawn from a cryptographic source rather than Random.
/// </summary>
public static class TemporaryPassword
{
    // No O/0, I/l/1, or S/5. A password that cannot be dictated over a phone gets written
    // down somewhere instead, which is worse than a slightly smaller alphabet.
    private const string Letters = "ABCDEFGHJKMNPQRTUVWXYZabcdefghijkmnpqrtuvwxyz";
    private const string Digits = "23456789";

    private const int LetterCount = 8;
    private const int DigitCount = 3;

    /// <summary>
    /// Returns a password that satisfies <see cref="PasswordPolicy"/> by construction: it
    /// always contains letters and digits, and is longer than the minimum.
    /// </summary>
    public static string Generate()
    {
        var characters = new char[LetterCount + DigitCount];

        for (var i = 0; i < LetterCount; i++)
            characters[i] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];

        for (var i = 0; i < DigitCount; i++)
            characters[LetterCount + i] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];

        Shuffle(characters);
        return new string(characters);
    }

    /// <summary>Fisher-Yates, drawing each index from the same cryptographic source.</summary>
    private static void Shuffle(char[] characters)
    {
        for (var i = characters.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (characters[i], characters[j]) = (characters[j], characters[i]);
        }
    }
}
