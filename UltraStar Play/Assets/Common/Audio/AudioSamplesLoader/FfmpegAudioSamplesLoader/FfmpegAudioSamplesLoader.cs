using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

public unsafe class FfmpegAudioSamplesLoader
{
    public void ConfigureFfmpeg(string rootPath)
    {
        ffmpeg.RootPath = rootPath;
    }

    public FfmpegAudioSamplesData Load(string filePath, int? targetSampleRate = null, int? targetChannels = null)
    {
        using (var format = new FormatContext(filePath))
        using (var codec = new CodecContext(format.AudioStream))
        {
            // Target format: Float
            const AVSampleFormat targetFormat = AVSampleFormat.AV_SAMPLE_FMT_FLT;
            
            // Use provided target sample rate/channels or fallback to codec's original values
            int finalSampleRate = targetSampleRate ?? codec.Context->sample_rate;
            int finalChannels = targetChannels ?? codec.Context->ch_layout.nb_channels;

            AVChannelLayout targetLayout = default;
            if (targetChannels.HasValue)
            {
                ffmpeg.av_channel_layout_default(&targetLayout, finalChannels);
            }
            else
            {
                Check(ffmpeg.av_channel_layout_copy(&targetLayout, &codec.Context->ch_layout), "Could not copy channel layout");
            }

            try
            {
                using (var packet = new Packet())
                using (var frame = new Frame())
                using (var swr = new SwrContext(&targetLayout, targetFormat, finalSampleRate, codec.Context))
                {
                    var allSamples = new List<float>();
                    var currentMaxTargetNbSamples = 4096;
                    byte* pOutputBuffer = null;
                    byte** ppOutputBuffer = &pOutputBuffer;
                    Check(ffmpeg.av_samples_alloc(ppOutputBuffer, null, finalChannels, currentMaxTargetNbSamples, targetFormat, 0), "Could not allocate samples");

                    try
                    {
                        while (ffmpeg.av_read_frame(format.Context, packet.Pointer) >= 0)
                        {
                            if (packet.Pointer->stream_index == format.AudioStream->index)
                            {
                                if (ffmpeg.avcodec_send_packet(codec.Context, packet.Pointer) >= 0)
                                {
                                    ReceiveAndResample(codec.Context, frame.Pointer, swr.Context, finalSampleRate, finalChannels, targetFormat, allSamples, ref pOutputBuffer, ref currentMaxTargetNbSamples);
                                }
                            }
                            ffmpeg.av_packet_unref(packet.Pointer);
                        }

                        ffmpeg.avcodec_send_packet(codec.Context, null);
                        ReceiveAndResample(codec.Context, frame.Pointer, swr.Context, finalSampleRate, finalChannels, targetFormat, allSamples, ref pOutputBuffer, ref currentMaxTargetNbSamples);

                        ReceiveAndResample(codec.Context, null, swr.Context, finalSampleRate, finalChannels, targetFormat, allSamples, ref pOutputBuffer, ref currentMaxTargetNbSamples);
                    }
                    finally
                    {
                        if (pOutputBuffer != null) ffmpeg.av_freep(&pOutputBuffer);
                    }

                    return new FfmpegAudioSamplesData
                    {
                        Samples = allSamples.ToArray(),
                        SampleRate = finalSampleRate,
                        Channels = finalChannels
                    };
                }
            }
            finally
            {
                ffmpeg.av_channel_layout_uninit(&targetLayout);
            }
        }
    }

    private sealed class FormatContext : IDisposable
    {
        public AVFormatContext* Context;
        public AVStream* AudioStream;

        public FormatContext(string filePath)
        {
            try
            {
                AVFormatContext* pFormatContext = null;
                Check(ffmpeg.avformat_open_input(&pFormatContext, filePath, null, null), "Could not open file");
                Context = pFormatContext;
                Check(ffmpeg.avformat_find_stream_info(Context, null), "Could not find stream info");

                for (var i = 0; i < Context->nb_streams; i++)
                {
                    if (Context->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                    {
                        AudioStream = Context->streams[i];
                        break;
                    }
                }
                if (AudioStream == null) throw new Exception("Could not find audio stream.");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (Context == null) return;
            var p = Context;
            ffmpeg.avformat_close_input(&p);
            Context = null;
        }
    }

    private sealed class CodecContext : IDisposable
    {
        public AVCodecContext* Context;

        public CodecContext(AVStream* pStream)
        {
            try
            {
                var pCodec = ffmpeg.avcodec_find_decoder(pStream->codecpar->codec_id);
                if (pCodec == null) throw new Exception("Could not find codec.");

                Context = ffmpeg.avcodec_alloc_context3(pCodec);
                if (Context == null) throw new Exception("Could not allocate codec context.");

                Check(ffmpeg.avcodec_parameters_to_context(Context, pStream->codecpar), "Could not copy codec parameters to context");
                Check(ffmpeg.avcodec_open2(Context, pCodec, null), "Could not open codec");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (Context == null) return;
            var p = Context;
            ffmpeg.avcodec_free_context(&p);
            Context = null;
        }
    }

    private sealed class Packet : IDisposable
    {
        public readonly AVPacket* Pointer;

        public Packet()
        {
            Pointer = ffmpeg.av_packet_alloc();
            if (Pointer == null) throw new Exception("Could not allocate packet.");
        }

        public void Dispose() { fixed (AVPacket** pp = &Pointer) ffmpeg.av_packet_free(pp); }
    }

    private sealed class Frame : IDisposable
    {
        public readonly AVFrame* Pointer;

        public Frame()
        {
            Pointer = ffmpeg.av_frame_alloc();
            if (Pointer == null) throw new Exception("Could not allocate frame.");
        }

        public void Dispose() { fixed (AVFrame** pp = &Pointer) ffmpeg.av_frame_free(pp); }
    }

    private sealed class SwrContext : IDisposable
    {
        public FFmpeg.AutoGen.SwrContext* Context;

        public SwrContext(AVChannelLayout* targetLayout, AVSampleFormat targetFormat, int targetSampleRate, AVCodecContext* pCodecContext)
        {
            try
            {
                FFmpeg.AutoGen.SwrContext* pSwrContext = null;
                Check(ffmpeg.swr_alloc_set_opts2(&pSwrContext, targetLayout, targetFormat, targetSampleRate,
                    &pCodecContext->ch_layout, pCodecContext->sample_fmt, pCodecContext->sample_rate, 0, null), "Could not allocate resampler context");
                Context = pSwrContext;
                Check(ffmpeg.swr_init(Context), "Could not initialize resampler context");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (Context == null) return;
            var p = Context;
            ffmpeg.swr_free(&p);
            Context = null;
        }
    }

    private void ReceiveAndResample(AVCodecContext* pCodecContext, AVFrame* pFrame, FFmpeg.AutoGen.SwrContext* pSwrContext,
        int targetSampleRate, int targetChannels, AVSampleFormat targetFormat, List<float> allSamples,
        ref byte* pOutputBuffer, ref int currentMaxTargetNbSamples)
    {
        if (pFrame == null) // Flush resampler
        {
            ResampleAndStore(pSwrContext, null, 0, targetSampleRate, pCodecContext->sample_rate, targetChannels, targetFormat, allSamples, ref pOutputBuffer, ref currentMaxTargetNbSamples);
            return;
        }

        while (ffmpeg.avcodec_receive_frame(pCodecContext, pFrame) >= 0)
        {
            try
            {
                ResampleAndStore(pSwrContext, (byte**)&pFrame->data, pFrame->nb_samples, targetSampleRate, pCodecContext->sample_rate, targetChannels, targetFormat, allSamples, ref pOutputBuffer, ref currentMaxTargetNbSamples);
            }
            finally
            {
                ffmpeg.av_frame_unref(pFrame);
            }
        }
    }

    private void ResampleAndStore(FFmpeg.AutoGen.SwrContext* pSwrContext, byte** pInputData, int inputNbSamples,
        int targetSampleRate, int inputSampleRate, int targetChannels, AVSampleFormat targetFormat, List<float> allSamples,
        ref byte* pOutputBuffer, ref int currentMaxTargetNbSamples)
    {
        var delay = ffmpeg.swr_get_delay(pSwrContext, inputSampleRate);
        var maxTargetNbSamples = (int)ffmpeg.av_rescale_rnd(delay + inputNbSamples, targetSampleRate, inputSampleRate, AVRounding.AV_ROUND_UP);

        if (maxTargetNbSamples > currentMaxTargetNbSamples)
        {
            fixed (byte** pp = &pOutputBuffer)
            {
                ffmpeg.av_freep(pp);
            }
            currentMaxTargetNbSamples = maxTargetNbSamples;
            fixed (byte** pp = &pOutputBuffer)
            {
                Check(ffmpeg.av_samples_alloc(pp, null, targetChannels, currentMaxTargetNbSamples, targetFormat, 0), "Could not reallocate samples");
            }
        }

        fixed (byte** pp = &pOutputBuffer)
        {
            var convertedSamples = ffmpeg.swr_convert(pSwrContext, pp, maxTargetNbSamples, pInputData, inputNbSamples);
            Check(convertedSamples, "Error during resampling");
            if (convertedSamples > 0)
            {
                var totalFloats = convertedSamples * targetChannels;
                var startCount = allSamples.Count;

                if (allSamples.Capacity < startCount + totalFloats)
                    allSamples.Capacity = Math.Max(allSamples.Capacity * 2, startCount + totalFloats);

                var pFloatBuffer = (float*)pOutputBuffer;
                for (var i = 0; i < totalFloats; i++)
                {
                    allSamples.Add(pFloatBuffer[i]);
                }
            }
        }
    }

    private static void Check(int errorCode, string message)
    {
        if (errorCode >= 0) return;
        var buffer = new byte[1024];
        fixed (byte* ptr = buffer)
        {
            ffmpeg.av_strerror(errorCode, ptr, (ulong)buffer.Length);
            var ffmpegError = System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            throw new Exception($"{message}: {ffmpegError} (Code: {errorCode})");
        }
    }
    
    public struct FfmpegAudioSamplesData
    {
        public float[] Samples { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
    }
}

