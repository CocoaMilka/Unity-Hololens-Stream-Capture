using SharpDX.Direct3D11;
using System;
using System.Runtime.InteropServices;
#if ENABLE_WINMD_SUPPORT
using Windows.Graphics.DirectX.Direct3D11;

public static class Direct3D11Helpers
{
    [ComImport]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface([In] ref Guid iid);
    }

    internal static readonly Guid IID_ID3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    public static IDirect3DSurface CreateDirect3DSurfaceFromSharpDXTexture(Texture2D texture)
    {
        var dxgiInterfaceAccess = (IDirect3DDxgiInterfaceAccess)Marshal.GetObjectForIUnknown(texture.NativePointer);

        Guid iid = IID_ID3D11Texture2D; // Create a local copy of the Guid
        IntPtr direct3DSurfacePtr = dxgiInterfaceAccess.GetInterface(ref iid);

        return (IDirect3DSurface)Marshal.GetObjectForIUnknown(direct3DSurfacePtr);
    }
}
#endif