using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;

namespace Core.Controllers
{
    class Scheduler
    {
        private Interfaces.ISimpleMediaPlayer mediaPlayer;
        private List<Models.ScheduleEntry> schedule;
        private Timer timer;
        private TimeSpan pausePosition;
        private FileInfo pausedFile;
        private bool isPlaying = false;
        private FileInfo currentFile;
        private Models.ScheduleEntry currentScheduleEntry; 

        public bool IsPlaying { get => isPlaying; }
        public Scheduler(Interfaces.ISimpleMediaPlayer mediaPlayer)
        {
            this.mediaPlayer = mediaPlayer;
            mediaPlayer.OnPlaybackCompleted += Main;
            timer = new Timer((o) => Main(this, new EventArgs()),null, Timeout.Infinite, Timeout.Infinite);
        }

        private FileInfo GetNextFile(FileInfo file, bool cycle = false)
        {
            if (file == null)
                return null;
            var files = file.Directory.GetFiles().OrderBy(x => x.Name).ToList();
            if (files.Count == 0)
            {
                mediaPlayer.DisplayError("Directory is empty!");
                return null;
            }
            var index = files.FindIndex(x => x.FullName == currentFile.FullName);
            //file not found
            if(index == -1)
            {
                return null;
            }
            //this file is last in the folder
            if (index == (files.Count - 1))
            {
                index = 0;
                if (!cycle)
                    return null;
            }
            else
                index++;
            return files[index];
        }

        private FileInfo GetNextFile(string path)
        {
            if (!Directory.Exists(path))
            {
                mediaPlayer.DisplayError("Directory doesn't exist!");
                return null;
            }
            var files = (new DirectoryInfo(path)).GetFiles().OrderBy(x => x.Name).ToList();
            if (files.Count == 0)
            {
                mediaPlayer.DisplayError("Directory is empty!");
                return null;
            }
            return files[0];
        }

        private void Main(object sender, EventArgs e)
        {
            if(schedule == null)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }
            //on finished playing file
            if(sender is System.Windows.Controls.MediaElement)
            {
                isPlaying = false;
                if(currentScheduleEntry?.Priority == 0)
                {
                    var nextFile = GetNextFile(currentFile, false);
                    if(nextFile != null)
                    {
                        currentFile = nextFile;
                        mediaPlayer.OpenFile(currentFile);
                        mediaPlayer.Play();
                        isPlaying = true;
                        return;
                    }
                    else
                    {
                        currentScheduleEntry = null;
                        if(pausedFile != null)
                        {
                            currentFile = pausedFile;
                            pausedFile = null;
                            mediaPlayer.OpenFile(currentFile);
                            mediaPlayer.SetMediaPosition(pausePosition);
                            mediaPlayer.Play();
                            isPlaying = true;
                            return;
                        }
                        else
                        {
                            currentFile = null;
                        }
                        //goto check current background
                    }
                }
                //check current background
                var currentBackground = schedule.Where(x => (x.Priority == 1) && (x.Start <= DateTime.Now.TimeOfDay) && (x.End > DateTime.Now.TimeOfDay)).FirstOrDefault();
                //nothing is scheduled 4 this moment
                if (currentBackground == null)
                    return;
                currentScheduleEntry = currentBackground;
                currentFile = GetNextFile(currentFile, true);
                //
                if(currentFile == null)
                    currentFile = GetNextFile(currentScheduleEntry.Path);
                if (currentFile == null)
                    return;
                mediaPlayer.OpenFile(currentFile);
                mediaPlayer.Play();
                isPlaying = true;
                return;
            }
            //on new schedule loaded
            if(sender is List<Models.ScheduleEntry>)
            {
                //schedule next period
                var nextScheduleEntry = schedule.FirstOrDefault(x => x.Start > DateTime.Now.TimeOfDay);
                var nearestBackgroundEnding = schedule.FirstOrDefault(x => x.Priority == 1 && x.End > DateTime.Now.TimeOfDay);
                if (nearestBackgroundEnding != null && (nextScheduleEntry == null || nearestBackgroundEnding.End < nextScheduleEntry.Start))
                    timer.Change((nearestBackgroundEnding.End - DateTime.Now.TimeOfDay), Timeout.InfiniteTimeSpan);
                else if (nextScheduleEntry == null)
                    timer.Change(((new TimeSpan(24, 0, 0)) - DateTime.Now.TimeOfDay + schedule.First().Start), Timeout.InfiniteTimeSpan);
                else
                    timer.Change((nextScheduleEntry.Start - DateTime.Now.TimeOfDay), Timeout.InfiniteTimeSpan);
                if (!isPlaying)
                {
                    //check current background
                    var currentBackground = schedule.Where(x => (x.Priority == 1) && (x.Start <= DateTime.Now.TimeOfDay) && (x.End > DateTime.Now.TimeOfDay)).FirstOrDefault();
                    //nothing is scheduled 4 this moment
                    if (currentBackground == null)
                        return;
                    currentScheduleEntry = currentBackground;
                    currentFile = GetNextFile(currentFile, true);
                    //
                    if (currentFile == null)
                        currentFile = GetNextFile(currentScheduleEntry.Path);
                    if (currentFile == null)
                        return;
                    mediaPlayer.OpenFile(currentFile);
                    mediaPlayer.Play();
                    isPlaying = true;
                    
                }
                return;
            }
            //on another entry time has come
            if(sender is Scheduler)
            {
                //setup timer
                var nextScheduleEntry = schedule.FirstOrDefault(x => x.Start > DateTime.Now.TimeOfDay);
                var nearestBackgroundEnding = schedule.FirstOrDefault(x => x.Priority == 1 && x.End > DateTime.Now.TimeOfDay);
                if (nearestBackgroundEnding != null && (nextScheduleEntry==null || nearestBackgroundEnding.End < nextScheduleEntry.Start))
                    timer.Change((nearestBackgroundEnding.End - DateTime.Now.TimeOfDay), Timeout.InfiniteTimeSpan);
                else if (nextScheduleEntry == null)
                    timer.Change(((new TimeSpan(24, 0, 0)) - DateTime.Now.TimeOfDay + schedule.First().Start), Timeout.InfiniteTimeSpan);
                else
                    timer.Change((nextScheduleEntry.Start - DateTime.Now.TimeOfDay), Timeout.InfiniteTimeSpan);
                //playing interrupt now, no actions needed
                if (currentScheduleEntry!=null && currentScheduleEntry.Priority == 0)
                    return;
                //checking what to do now
                var backgroundCandidate = schedule.Where(x => (x.Priority == 1) && (x.Start <= DateTime.Now.TimeOfDay) && (x.End > DateTime.Now.TimeOfDay)).FirstOrDefault();
                var interruptCandidate = schedule.Where(x => (x.Priority == 0) && (x.Start.Hours == DateTime.Now.TimeOfDay.Hours) && (x.Start.Minutes == DateTime.Now.TimeOfDay.Minutes)).FirstOrDefault();
                if (interruptCandidate != null)
                {
                    if (isPlaying)
                    {
                        pausedFile = currentFile;
                        pausePosition = mediaPlayer.GetMediaPosition();
                    }
                    currentScheduleEntry = interruptCandidate;
                    var nextFile = GetNextFile(currentScheduleEntry.Path);
                    if (nextFile != null)
                    {
                        currentFile = nextFile;
                        mediaPlayer.OpenFile(currentFile);
                        mediaPlayer.Play();
                        isPlaying = true;
                        return;
                    }
                }
                if(backgroundCandidate != null)
                {
                    currentScheduleEntry = backgroundCandidate;
                        currentFile = GetNextFile(currentScheduleEntry.Path);
                    if (currentFile == null)
                        return;
                    mediaPlayer.OpenFile(currentFile);
                    mediaPlayer.SetMediaPosition(pausePosition);
                    mediaPlayer.Play();
                    isPlaying = true;
                }
                return;
            }
        }

        public void LoadSchedule(FileInfo file)
        {
            schedule = ScheduleParser.ParseTxt(file);
            if (schedule == null)
            {
                mediaPlayer.DisplayError("An error occured while trying to load schedule from file");
                return;
            }
            Main(schedule, new EventArgs());
        }
        ~Scheduler()
        {
            timer.Dispose();
        }

    }
}
