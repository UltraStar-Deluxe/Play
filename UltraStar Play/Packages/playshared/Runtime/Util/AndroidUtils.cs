using System;
using UnityEngine;

public static class AndroidUtils
{
    /**
     * Returns the path where this app can store data.
     *
     * Example return value: /storage/emulated/0/Android/data/com.UltraStarPlay.UltraStarPlay/files
     *
     * @param sdCard if true, the path on the SD card is returned. Otherwise the path on non-removable memory hardware is returned.
     */
	public static string GetAppSpecificStorageAbsolutePath(bool sdCard)
    {
        // Note: Android uses the terms "internal storage", "primary external storage", and "secondary external storage".
        // - internal storage: General storage, includes storage of other apps. Requires root access.
        // - primary external storage: The app specific storage folder on the device (non-removable).
        // - secondary external storage: The app specific storage folder on the sd card.
#if UNITY_ANDROID
        if (Application.isEditor)
        {
            // Return dummy values for testing.
            return sdCard
                ? "/storage/sdcard/0/Android/data/UltraStarPlay/files"
                : "/storage/emulated/0/Android/data/UltraStarPlay/files";
        }

        using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        using AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment");

        // Get all available external file directories (emulated and sdCards)
        AndroidJavaObject[] externalFilesDirectories = context.Call<AndroidJavaObject[]>("getExternalFilesDirs", (object)null);
        for (int i = 0; i < externalFilesDirectories.Length; i++)
        {
            AndroidJavaObject directory = externalFilesDirectories[i];
            string path = directory.Call<string>("getAbsolutePath");

            // Check which one is the SD-Card.
            bool isRemovable = environment.CallStatic<bool>("isExternalStorageRemovable", directory);
            bool isEmulated = environment.CallStatic<bool>("isExternalStorageEmulated", directory);
            if (isEmulated
                && !sdCard)
            {
                return path;
            }
            else if (isRemovable
                     && !isEmulated
                     && sdCard)
            {
                return path;
            }
        }

        // No storage found.
        return "";
#else
        return "";
#endif
    }

    /**
     * Returns the path of the storage folder relative to the root path.
     * This is the path when browsing the device for example on Windows.
     *
     * Example return value: Android/data/com.UltraStarPlay.UltraStarPlay/files
     */
    public static string GetAppSpecificStorageRelativePath(bool sdCard)
    {
        string appSpecificStorageFolder = GetAppSpecificStorageAbsolutePath(sdCard);
        if (appSpecificStorageFolder.IsNullOrEmpty())
        {
            return "";
        }

        string storageRootPath = GetStorageRootPath(sdCard);
        if (storageRootPath.IsNullOrEmpty()
            || appSpecificStorageFolder.Length <= storageRootPath.Length + 1)
        {
            return "";
        }
        return appSpecificStorageFolder.Substring(storageRootPath.Length + 1);
    }

    /**
     * Returns the root path of the storage folder.
     * Android mounts the storage at this path.
     *
     * Example return value: /storage/emulated/0
     */
    public static string GetStorageRootPath(bool sdCard)
    {
        string appSpecificStorageFolder = GetAppSpecificStorageAbsolutePath(sdCard);
        if (appSpecificStorageFolder.IsNullOrEmpty())
        {
            return "";
        }
        int androidIndex = appSpecificStorageFolder.IndexOf("/Android/", StringComparison.InvariantCulture);
        if (androidIndex >= 0)
        {
            return appSpecificStorageFolder.Substring(0, androidIndex);
        }

        return "";
    }
}
