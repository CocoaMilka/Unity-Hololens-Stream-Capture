using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

// holy shit why can't I just use these in the editor
#if ENABLE_WINMD_SUPPORT
    using FFmpegInteropX;
    using Windows.Foundation;
    using Windows.Media.Core;
    using Windows.Media.Playback;
    using Windows.Graphics.Imaging;
    using Windows.Storage.Streams;
#endif

public class RenderStream : MonoBehaviour
{
    public UnityEngine.UI.RawImage target;
    SharpDX.Direct3D11.Texture2D m_DstTexture;
    SharpDX.Direct3D11.Device device;
    SharpDX.Direct3D11.DeviceContext deviceContext;

    void Start()
    {
        // Dummy texture hack to get device and context from Unity
        // If this were a normal directx app, we would create the device and context ourselves, but unity creates its own so we must use it instead
        UnityEngine.Texture2D targetX = new UnityEngine.Texture2D(512, 512, TextureFormat.BGRA32, false);
        IntPtr texturePtr = targetX.GetNativeTexturePtr();
        SharpDX.Direct3D11.Texture2D dstTextureX = new SharpDX.Direct3D11.Texture2D(texturePtr);

        // Create DirectX device and context from texture
        device = dstTextureX.Device;
        deviceContext = device.ImmediateContext;

        // Create shared texture for FFmpeg and Unity to access
        SharpDX.Direct3D11.Texture2DDescription sharedTexture2DDescription = dstTextureX.Description;
        sharedTexture2DDescription.OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.Shared;
        m_DstTexture = new SharpDX.Direct3D11.Texture2D(device, sharedTexture2DDescription);

        // Get Shader Resource View to shared texture
        var d3d11ShaderResourceView = new ShaderResourceView(device, m_DstTexture);

        // Assign the shader resource view to the target RawImage
        target.texture = UnityEngine.Texture2D.CreateExternalTexture(512, 512, TextureFormat.BGRA32, false, false, texturePtr);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
