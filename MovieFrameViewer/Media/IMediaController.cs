using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MovieFrameViewer.Media
{
    interface IMediaController : IDisposable
    {
        TimeSpan PlayTime { get; }
        int CurrentFrameIndex { get; }
        int TotalFrame { get; }
        Size Resolution { get; }
        double Fps { get; }
        void Initialize(MediaStateData stateData);

        void Load(string path);
        void Close();
        void Play();
        void Stop();
        void Pause();

        bool GetFrame(int frameIndex);
        List<MediaFrameImage> CreateNeighborinFrames(Range<int> frameRange);
    }
}
