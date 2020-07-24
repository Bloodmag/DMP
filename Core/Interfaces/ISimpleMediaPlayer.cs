using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    interface ISimpleMediaPlayer
    {
        bool OpenFile(System.IO.FileInfo file);
        bool Pause();
        bool Play();
        bool StopPlayback();
        bool SetMediaPosition(TimeSpan time);
        TimeSpan GetMediaPosition();
        void DisplayError(string message);
        event EventHandler OnPlaybackCompleted;
    }
}
