using FrierenHR.Application.Common.Security;

namespace FrierenHR.Tests.Security;

public class PasswordHasherTests
{
    [Fact]
    public void A_hashed_password_verifies_against_itself()
    {
        var hash = PasswordHasher.Hash("correct horse 7");

        Assert.True(PasswordHasher.Verify("correct horse 7", hash));
    }

    [Fact]
    public void A_wrong_password_does_not_verify()
    {
        var hash = PasswordHasher.Hash("correct horse 7");

        Assert.False(PasswordHasher.Verify("correct horse 8", hash));
    }

    [Fact]
    public void Verification_is_case_sensitive()
    {
        var hash = PasswordHasher.Hash("Passw0rd");

        Assert.False(PasswordHasher.Verify("passw0rd", hash));
    }

    [Fact]
    public void The_same_password_hashes_differently_every_time()
    {
        // Per-hash salt. Two users with the same password must not share a hash.
        Assert.NotEqual(PasswordHasher.Hash("Passw0rd"), PasswordHasher.Hash("Passw0rd"));
    }

    [Fact]
    public void The_hash_records_its_iteration_count_and_salt()
    {
        // Format is iterations.salt.hash, which is what lets the count be raised later without invalidating existing hashes.
        var parts = PasswordHasher.Hash("Passw0rd").Split('.');

        Assert.Equal(3, parts.Length);
        Assert.Equal(100_000, int.Parse(parts[0]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("nonsense")]
    [InlineData("only.two")]
    public void A_malformed_stored_hash_is_rejected_rather_than_throwing(string stored)
    {
        Assert.False(PasswordHasher.Verify("Passw0rd", stored));
    }

    [Fact]
    public void An_empty_password_still_round_trips()
    {
        // The policy rejects this on the way in; the hasher itself should not throw on it.
        var hash = PasswordHasher.Hash("");

        Assert.True(PasswordHasher.Verify("", hash));
        Assert.False(PasswordHasher.Verify("x", hash));
    }
}

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Passw0rd")]
    [InlineData("12345678a")]
    [InlineData("a1234567")]
    public void A_password_with_eight_characters_a_letter_and_a_digit_is_accepted(string password)
    {
        Assert.Empty(PasswordPolicy.GetErrors(password));
    }

    [Fact]
    public void Too_short_is_rejected()
    {
        var errors = PasswordPolicy.GetErrors("Pass0rd");

        Assert.Contains(errors, e => e.Contains("8 characters"));
    }

    [Fact]
    public void Letters_only_is_rejected()
    {
        Assert.Contains(PasswordPolicy.GetErrors("passwordd"), e => e.Contains("number"));
    }

    [Fact]
    public void Digits_only_is_rejected()
    {
        Assert.Contains(PasswordPolicy.GetErrors("123456789"), e => e.Contains("letter"));
    }

    [Fact]
    public void Null_and_empty_collect_every_error()
    {
        Assert.Equal(3, PasswordPolicy.GetErrors(null).Count);
        Assert.Equal(3, PasswordPolicy.GetErrors("").Count);
    }

    [Fact]
    public void Validate_throws_with_every_reason_joined()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PasswordPolicy.Validate("abc"));

        Assert.Contains("8 characters", ex.Message);
        Assert.Contains("number", ex.Message);
    }

    [Fact]
    public void Validate_accepts_a_valid_password()
    {
        PasswordPolicy.Validate("Passw0rd");
    }
}
