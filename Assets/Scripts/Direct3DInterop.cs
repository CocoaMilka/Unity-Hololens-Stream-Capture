#if ENABLE_WINMD_SUPPORT
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Windows.Graphics.DirectX.Direct3D11;

public class Direct3DInterop
{
    // Define the P/Invoke signature for CreateDirect3D11SurfaceFromDXGISurface
    [DllImport("Windows.Graphics.DirectX.Direct3D11.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    //private static extern int CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr direct3DSurface);
    public static extern int CreateDirect3DSurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr direct3DSurface);

    // This method creates a Direct3D11 Surface from a Unity Texture2D
    public static IDirect3DSurface CreateDirect3DSurfaceFromUnityTexture(Texture2D unityTexture)
    {
        if (unityTexture == null)
            throw new ArgumentNullException(nameof(unityTexture));

        // Get the native Direct3D texture pointer from the Unity texture
        IntPtr nativeTexturePtr = unityTexture.GetNativeTexturePtr();

        // Marshal the native pointer into a usable Direct3D11 surface
        IntPtr surfacePointer;
        int hr = CreateDirect3DSurfaceFromDXGISurface(nativeTexturePtr, out surfacePointer);

        if (hr != 0) // Check if HRESULT is a failure
        {
            throw new InvalidOperationException($"Failed to create Direct3DSurface. HRESULT: 0x{hr:X}");
        }

        // Marshal the IntPtr to IDirect3DSurface using Windows Runtime interop
        return Marshal.GetObjectForIUnknown(surfacePointer) as IDirect3DSurface;
    }
}
#endif