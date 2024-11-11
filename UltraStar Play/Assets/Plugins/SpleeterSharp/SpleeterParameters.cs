namespace SpleeterSharp
{
    public class SpleeterParameters
    {
        /**
         * The audio file that should be processed
         */
        public string InputFile { get; set; }

        /**
         *  Path of the output directory to write audio files
         */
        public string OutputFolder { get; set; }

        /**
         * Audio codec to be used (wav|mp3|ogg|m4a|wma|flac) for the separated output
         * [default: wav]
         */
        public string OutputFileCodec { get; set; }

        /**
         * Audio bitrate to be used for the separated output
         * [default: 128k]
         */
        public string OutputFileBitrate { get; set; }

        /**
         * Name of the audio adapter to use for audio I/O
         * [default: spleeter.audio.ffmpeg.FFMPEGProcessAudioAdapter]
         */
        public string AudioAdapter { get; set; }


        public string ParamsFileName { get; set; }

        /**
         * Set a maximum duration for processing audio
         * (only separate offset + duration first seconds of the input file)
         * [default: 600.0]
         */
        public float MaxDuration { get; set; }

        /**
         *  Set the starting offset to separate audio from  [default: 0.0]
         */
        public float Offset { get; set; }

        /**
         * Template string that will be formatted to generatedoutput filename.
         * Such template should be Python formattablestring,
         * and could use {filename}, {instrument}, and {codec} variables
         * [default: {filename}/{instrument}.{codec}]
         */
        public string FileNameFormat { get; set; }

        /**
         * Whether to use multichannel Wiener filtering for separation
         * [default: False]
         */
        public bool MultiChannelWienerFiltering { get; set; }
        
        /**
         * Overwrite existing files. Can be used with SpleeterMsvcExe.
         */
        public bool Overwrite { get; set; }
    }
}
