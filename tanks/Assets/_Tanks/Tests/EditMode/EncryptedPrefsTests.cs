using System;
using NUnit.Framework;
using Tanks.ApiClient.Model;
using Tanks.Complete.Persistence;
using Tanks.Complete.Persistence.Models;
using UnityEngine;

namespace Tanks.Tests.Persistence
{
    public class EncryptedPrefsTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void SetString_EncryptsAndDecrypts_RoundTrip()
        {
            const string key = "encrypted_test";
            const string value = "super-secret-value";

            EncryptedPrefs.SetString(key, value);

            Assert.AreNotEqual(value, PlayerPrefs.GetString(key), "Stored value should be encrypted.");
            Assert.AreEqual(value, EncryptedPrefs.GetString(key));
        }

        [Test]
        public void GetString_InvalidCipher_ReturnsDefault()
        {
            const string key = "invalid_cipher";
            PlayerPrefs.SetString(key, "not-base64");

            var result = EncryptedPrefs.GetString(key, "fallback");

            Assert.AreEqual("fallback", result);
        }
    }

    public class AuthDataModelTests
    {
        [Test]
        public void AuthTokens_FromAuthResponse_MapsFields()
        {
            var now = DateTimeOffset.UtcNow;
            var response = new AuthResponse(
                new AuthResponseUser(1, "alice", 2, 3, now.ToString("o"), now.ToString("o")),
                "access-token",
                "refresh-token",
                now.AddMinutes(10).ToString("o"),
                now.AddHours(1).ToString("o"));

            var tokens = AuthTokens.FromAuthResponse(response);

            Assert.IsTrue(tokens.IsAccessTokenValid(TimeSpan.Zero));
            Assert.IsTrue(tokens.IsRefreshTokenValid(TimeSpan.Zero));
            Assert.AreEqual("refresh-token", tokens.RefreshToken);
        }

        [Test]
        public void AuthTokens_ExpiredAccessToken_IsInvalid()
        {
            var tokens = new AuthTokens
            {
                AccessToken = "expired",
                RefreshToken = "still-good",
                AccessTokenExpiresAtUnixSeconds = DateTimeOffset.UtcNow.AddSeconds(-5).ToUnixTimeSeconds(),
                RefreshTokenExpiresAtUnixSeconds = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds()
            };

            Assert.IsFalse(tokens.IsAccessTokenValid(TimeSpan.Zero));
            Assert.IsTrue(tokens.IsRefreshTokenValid(TimeSpan.Zero));
        }

        [Test]
        public void UserData_FromAuthResponse_PopulatesFields()
        {
            var nowIso = DateTimeOffset.UtcNow.ToString("o");
            var response = new AuthResponse(
                new AuthResponseUser(42, "bob", 7, 1, nowIso, nowIso),
                "a",
                "b",
                nowIso,
                nowIso);

            var data = UserData.FromAuthResponse(response);

            Assert.IsTrue(data.IsValid);
            Assert.AreEqual(42, data.UserId);
            Assert.AreEqual("bob", data.UserName);
            Assert.AreEqual(7, data.Wins);
            Assert.AreEqual(1, data.Losses);
        }
    }
}
