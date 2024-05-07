using NUnit.Framework;

public class SettingsUtilsTest
{
    [Test]
    // Should return default if unknown
    [TestCase(EKnownUltraStarSongFormatVersion.V110, EUpgradeUltraStarSongFormatVersion.V120, "WeirdVersion", "1.1.0")]
    // Should upgrade
    [TestCase(EKnownUltraStarSongFormatVersion.V110, EUpgradeUltraStarSongFormatVersion.V120, "", "1.2.0")]
    [TestCase(EKnownUltraStarSongFormatVersion.V110, EUpgradeUltraStarSongFormatVersion.V120, "1.0.0", "1.2.0")]
    [TestCase(EKnownUltraStarSongFormatVersion.V110, EUpgradeUltraStarSongFormatVersion.V120, "1.1.0", "1.2.0")]
    // Should not downgrade
    [TestCase(EKnownUltraStarSongFormatVersion.V120, EUpgradeUltraStarSongFormatVersion.V120, "2.0.0", "2.0.0")]
    [TestCase(EKnownUltraStarSongFormatVersion.V110, EUpgradeUltraStarSongFormatVersion.None, "1.0.0", "1.0.0")]
    public void ShouldReturnUltraStarSongFormatVersionForSave(
        EKnownUltraStarSongFormatVersion defaultVersion,
        EUpgradeUltraStarSongFormatVersion upgradeVersion,
        string currentVersion,
        string expectedVersion)
    {
        Settings testSettings = new Settings()
        {
            DefaultUltraStarSongFormatVersionForSave = defaultVersion,
            UpgradeUltraStarSongFormatVersionForSave = upgradeVersion,
        };
        UltraStarSongFormatVersion actualVersion = SettingsUtils.GetUltraStarSongFormatVersionForSave(testSettings, new UltraStarSongFormatVersion(currentVersion));
        Assert.AreEqual(new UltraStarSongFormatVersion(expectedVersion), actualVersion);
    }
}
