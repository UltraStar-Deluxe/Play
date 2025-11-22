using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;
using LibVLCSharp;

namespace LibVLCSharp
{
    public static class TextureHelper
    {
        const string UnityPlugin = "VLCUnityPlugin";

        [DllImport(UnityPlugin, CallingConvention = CallingConvention.Cdecl, EntryPoint = "libvlc_unity_set_bit_depth_format")]
        static extern void SetBitDepthFormat(IntPtr mediaplayer, int bitDepth);

        /// <summary>
        /// Helper for native texture creation
        /// </summary>
        /// <param name="player">mediaplayer instance</param>
        /// <param name="bitDepth">8 or 16 bits (16 bits is Windows-only for now)</param>
        /// <param name="linear">true for linear color space</param>
        /// <param name="mipmap">default to false</param>
        /// <returns>texture or null / throw if fails</returns>
        public static Texture2D CreateNativeTexture(ref MediaPlayer player, bool linear, BitDepth bitDepth = BitDepth.Bit8, bool mipmap = false)
        {
            uint width = 0;
            uint height = 0;

            player.Size(0, ref width, ref height);

            if (bitDepth == BitDepth.Bit16)
            {
                if (!SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
                {
                    throw new VLCException("16 bits was requested, but TextureFormat.RGBAHalf is not supported by your GPU");
                }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
                SetBitDepthFormat(player.NativeReference, (int)BitDepth.Bit16);
#endif
            }
            else
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
                SetBitDepthFormat(player.NativeReference, (int)BitDepth.Bit8);
#endif
            }

            var texptr = player.GetTexture(width, height, out bool updated);

            if (width != 0 && height != 0 && updated && texptr != IntPtr.Zero)
            {
                return Texture2D.CreateExternalTexture((int)width,
                        (int)height,
                        bitDepth == BitDepth.Bit16 ? TextureFormat.RGBAHalf : TextureFormat.RGBA32,
                        mipmap,
                        linear,
                        texptr);
            }
            else
            {
                return default;
            }
        }
    }

    public enum BitDepth
    {
        Bit8 = 8,
        Bit16 = 16
    }
}