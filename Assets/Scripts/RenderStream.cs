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
    using Windows.Graphics.DirectX.Direct3D11;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.UI.Xaml;
#endif

public class RenderStream : MonoBehaviour
{
    public UnityEngine.UI.RawImage target;
    SharpDX.Direct3D11.Texture2D m_DstTexture;
    SharpDX.Direct3D11.Device device;
    SharpDX.Direct3D11.DeviceContext deviceContext;

    #if ENABLE_WINMD_SUPPORT
        private MediaPlayer mediaPlayer;
        private MediaPlaybackItem mediaPlaybackItem;
        private FFmpegMediaSource ffmpegMediaSource;
        private SoftwareBitmap frameServerDest = new SoftwareBitmap(BitmapPixelFormat.Rgba8, 512, 512, BitmapAlphaMode.Premultiplied );
        private UnityEngine.Texture2D tex;
        private CanvasDevice canvasDevice;
        private IDirect3DSurface surface;
    #endif

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

        // Init ffmpeg for streaming
        #if ENABLE_WINMD_SUPPORT
            InitializeMediaPlayer();
        #endif
    }

#if ENABLE_WINMD_SUPPORT
    private async void InitializeMediaPlayer()
    {
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

            // Create FFmpegMediaSource from  sample stream
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
    }

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
        Debug.Log("Video frame available!");
    }
#endif

    // Update is called once per frame
    void Update()
    {
        
    }
}
