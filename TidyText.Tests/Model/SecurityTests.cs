using System;
using System.IO;
using NUnit.Framework;
using TidyText.Domain.Security;
using TidyText.Infrastructure.Security;

namespace TidyText.Tests.Model
{
    [TestFixture]
    public class SecurityTests
    {
        private string _tempDir = string.Empty;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "TidyText_SecurityTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void SecureKeyVault_Can_Save_And_Load_Keys()
        {
            var vault = new SecureKeyVault(_tempDir);
            vault.SetKey("Gemini", "secret_key_123");
            
            Assert.That(vault.GetKey("Gemini"), Is.EqualTo("secret_key_123"));

            // Create a new instance pointing to the same folder to test Load()
            var vault2 = new SecureKeyVault(_tempDir);
            Assert.That(vault2.GetKey("Gemini"), Is.EqualTo("secret_key_123"));
        }

        [Test]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void SecureKeyVault_Returns_Empty_For_Missing_Key()
        {
            var vault = new SecureKeyVault(_tempDir);
            Assert.That(vault.GetKey("NonExistent"), Is.EqualTo(string.Empty));
        }

        [Test]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void SecureKeyVault_Can_Remove_Key()
        {
            var vault = new SecureKeyVault(_tempDir);
            vault.SetKey("Gemini", "secret_key_123");
            Assert.That(vault.GetKey("Gemini"), Is.EqualTo("secret_key_123"));
            
            vault.RemoveKey("Gemini");
            Assert.That(vault.GetKey("Gemini"), Is.EqualTo(string.Empty));

            // Verify it was removed from disk
            var vault2 = new SecureKeyVault(_tempDir);
            Assert.That(vault2.GetKey("Gemini"), Is.EqualTo(string.Empty));
        }

        [Test]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void SecureKeyVault_Survives_Corrupted_File()
        {
            var vaultFile = Path.Combine(_tempDir, "keys.dat");
            File.WriteAllText(vaultFile, "not encrypted valid data");

            // Should not crash, should just start empty
            var vault = new SecureKeyVault(_tempDir);
            Assert.That(vault.GetKey("Gemini"), Is.EqualTo(string.Empty));
            
            // Should be able to save over the corrupted file
            vault.SetKey("Gemini", "new_key");
            Assert.That(vault.GetKey("Gemini"), Is.EqualTo("new_key"));
        }
    }
}
