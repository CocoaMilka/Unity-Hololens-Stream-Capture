using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Resource = SharpDX.DXGI.Resource;
using Texture2D = SharpDX.Direct3D11.Texture2D;

#if ENABLE_WINMD_SUPPORT
    using FFmpegInteropX;
    using Windows.Graphics.DirectX.Direct3D11;
    using Windows.Graphics.Imaging;
    using Windows.Media.Playback;
#endif

public class RenderStream : MonoBehaviour
{
    public UnityEngine.UI.RawImage target;
    private UnityEngine.Texture2D targetX;
    private IntPtr sharedHandle;

    SharpDX.Direct3D11.Texture2D m_DstTexture;
    SharpDX.Direct3D11.Device device;
    SharpDX.Direct3D11.DeviceContext deviceContext;

    private Surface surfaceX;

#if ENABLE_WINMD_SUPPORT
    private MediaPlayer mediaPlayer;
    private MediaPlaybackItem mediaPlaybackItem;
    private FFmpegMediaSource ffmpegMediaSource;
    private SoftwareBitmap frameServerDest = new SoftwareBitmap(BitmapPixelFormat.Rgba8, 512, 512, BitmapAlphaMode.Premultiplied );
    private IDirect3DSurface direct3DSurface;
#endif

    void Start()
    {
        // Dummy texture hack to get device and context from Unity
        // If this were a normal directx app, we would create the device and context ourselves, but unity creates its own so we must use it instead
        targetX = new UnityEngine.Texture2D(512, 512, TextureFormat.BGRA32, false);
        IntPtr texturePtr = targetX.GetNativeTexturePtr();
        SharpDX.Direct3D11.Texture2D dstTextureX = new SharpDX.Direct3D11.Texture2D(texturePtr);

        // Create DirectX device and context from texture
        device = dstTextureX.Device;
        deviceContext = device.ImmediateContext;

        // Create shared texture for FFmpeg and Unity to access
        Texture2DDescription sharedTextureDesc = new SharpDX.Direct3D11.Texture2DDescription
        {
            Width = 512,
            Height = 512,
            MipLevels = 1,
            ArraySize = 1,
            Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
            SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
            Usage = SharpDX.Direct3D11.ResourceUsage.Default,
            BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource | SharpDX.Direct3D11.BindFlags.RenderTarget,
            CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
            OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.Shared
        };
        // Textures are stored on GPU vram, surfaces are stored in ram
        Texture2D sharedTexture = new SharpDX.Direct3D11.Texture2D(device, sharedTextureDesc);
        surfaceX = sharedTexture.QueryInterface<Surface>();

        #if ENABLE_WINMD_SUPPORT
            Debug.Log("Converting SharpDX Surface to IDirect3DSurface");
            // Converts SharpDX Surface into Direct3DSurface
            direct3DSurface = Direct3D11Helpers.CreateDirect3DSurfaceFromSharpDXTexture(sharedTexture);
        #endif

        // Get shared handle from Direct3D texture
        Resource resource = sharedTexture.QueryInterface<SharpDX.DXGI.Resource>();
        sharedHandle = resource.SharedHandle;

        // Update Unity Texture with shared handle
        targetX.UpdateExternalTexture(sharedHandle);

        // Assign updated texture to RawImage
        target.texture = targetX;

        // Init ffmpeg for streaming

        InitializeMediaPlayer();
        //InitializeDirect3DSurface();
    }

    private async void InitializeMediaPlayer()
    {
#if ENABLE_WINMD_SUPPORT
        try
        {
            FFmpegInteropLogging.SetDefaultLogProvider();

            MediaSourceConfig configuration = new MediaSourceConfig()
            {
                MaxVideoThreads = 8,
                SkipErrors = uint.MaxValue,
                ReadAheadBufferDuration = TimeSpan.Zero,
                FastSeek = true,
                VideoDecoderMode = VideoDecoderMode.ForceFFmpegSoftwareDecoder
            };
            Debug.Log("FFmpegInteropX configuration set up successfully.");

            // Sample stream source
            string uri = "https://test-videos.co.uk/vids/sintel/mp4/h264/720/Sintel_720_10s_1MB.mp4";
            //string uri = "udp://@192.168.10.1:11111";

            // Create FFmpegMediaSource from sample stream
            Debug.Log($"Attempting to create media source from URI: {uri}");
            ffmpegMediaSource = await FFmpegMediaSource.CreateFromUriAsync(uri, configuration);
            if (ffmpegMediaSource == null) { throw new Exception("Failed to create FFmpegMediaSource."); } 
            Debug.Log("FFmpegMediaSource created successfully.");

            // Ensure video stream is valid, display video stream information for debug
            if (ffmpegMediaSource.CurrentVideoStream == null) { throw new Exception("CurrentVideoStream is null."); }
            Debug.Log($"VideoStream Info: CodecName={ffmpegMediaSource.CurrentVideoStream.CodecName}, DecoderEngine={ffmpegMediaSource.CurrentVideoStream.DecoderEngine}, HardwareDecoderStatus={ffmpegMediaSource.CurrentVideoStream.HardwareDecoderStatus}, Resolution={ffmpegMediaSource.CurrentVideoStream.PixelWidth}x{ffmpegMediaSource.CurrentVideoStream.PixelHeight}");

            // Create MediaPlaybackItem from FFmpegMediaSource
            mediaPlaybackItem = ffmpegMediaSource.CreateMediaPlaybackItem();
            if (mediaPlaybackItem == null) { throw new Exception("Failed to create MediaPlaybackItem."); }
            Debug.Log("MediaPlaybackItem created successfully.");

            mediaPlayer = new MediaPlayer
            {
                Source = mediaPlaybackItem,
                IsVideoFrameServerEnabled = true
            };

            // Subscribe to media events (mostly for debugging)
            // VideoFrameAvailable is where frame processing happens
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
            Debug.Log("MediaPlayer source set successfully.");

            mediaPlayer.Play();
            Debug.Log("MediaPlayer started playing.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception while setting up FFmpegInteropX: {ex.Message}");
        }
#endif
    }

#if ENABLE_WINMD_SUPPORT
    private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        Debug.Log("MediaPlayer opened media successfully.");
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Debug.LogError($"MediaPlayer failed: {args.Error}");
        Debug.LogError($"Extended Error Code: {args.ExtendedErrorCode}");
        Debug.LogError($"Error Message: {args.ErrorMessage}");
    }

    private async void MediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
    {
        try
        {
            //sender.CopyFrameToVideoSurface(surface);
            sender.CopyFrameToVideoSurface(direct3DSurface);
            // Notify Unity to update the texture with the new content.
            targetX.UpdateExternalTexture(sharedHandle);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception during VideoFrameAvailable: {ex.Message}");
        }
    }

#endif

    // Update is called once per frame
    void Update()
    {
        
    }
}
