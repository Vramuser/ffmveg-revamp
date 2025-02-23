using System;
using System.IO;
using ScriptPortal.Vegas;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;

class EntryPoint
{
    private const bool CONSOLE_SHOW = true; // Toggle console; "true" or "false" statement.

    public void FromVegas(Vegas vegas)
    {
        bool only_selected = true; // Process only selected video events
        string proxy_file_prefix = "PROXY-";
        string ffmpeg_path = "ffmpeg.exe";
        string ff_base_args = "-loglevel error -stats";
        string ff_filter_args = "-vf fps=60,scale=960:540:flags=neighbor";
        string ff_video_args = "-c:v libx264 -preset veryfast -crf 25 -g 60 -x264-params bframes=0 -maxrate 100M -bufsize 10M";
        string ff_audio_args = "-an";

        if (GetFullPath(ffmpeg_path) == null)
        {
            MessageBox.Show("VEGAS-PROXY: ffmpeg.exe not found! Ensure it’s installed and in PATH.\n\nSee https://ctt.cx/ffmpeg for installation instructions.");
            return;
        }

        int processedCount = 0;
        foreach (Track track in vegas.Project.Tracks)
        {
            if (track.IsValid() && track.IsVideo() && (only_selected || track.Selected))
            {
                foreach (TrackEvent trackEvent in track.Events)
                {
                    if (!trackEvent.Selected || !trackEvent.IsVideo()) continue;

                    VideoEvent videoEvent = (VideoEvent)trackEvent;
                    string currentMediaPath = Path.GetFullPath(videoEvent.ActiveTake.MediaPath);
                    string fileName = Path.GetFileName(currentMediaPath);
                    string originalDirectory = Path.GetDirectoryName(currentMediaPath);
                    
                    bool isInProxyFolder = Path.GetFileName(originalDirectory).ToLower() == "proxy"; // Checker
                    string proxyDirectory = isInProxyFolder ? originalDirectory : Path.Combine(originalDirectory, "proxy");
                    
                    // Ensure proxy folder exists and is visible
                    if (!isInProxyFolder && !Directory.Exists(proxyDirectory))
                    {
                        Directory.CreateDirectory(proxyDirectory);
                        File.SetAttributes(proxyDirectory, FileAttributes.Normal);
                    }

                    if (!File.Exists(currentMediaPath)) continue;

                    if (fileName.StartsWith(proxy_file_prefix)) //Checks if it has a prefix
                    {
                        string originalPath = Path.Combine(Directory.GetParent(originalDirectory).FullName, Regex.Replace(fileName, "^" + proxy_file_prefix, ""));
                        if (File.Exists(originalPath))
                        {
                            processedCount++;
                            ReplaceVideo(videoEvent, originalPath);
                        }
                        else if (CONSOLE_SHOW)
                        {
                            Console.WriteLine("Original file not found: " + originalPath);
                        }
                    }
                    else
                    {
                        string proxyPath = Path.Combine(proxyDirectory, proxy_file_prefix + fileName);

                        if (File.Exists(proxyPath))
                        {
                            if (new FileInfo(proxyPath).Length == 0)
                            {
                                if (CONSOLE_SHOW) Console.WriteLine("Deleting empty/corrupted file: " + proxyPath);
                                File.Delete(proxyPath);
                            }
                        }
                        else
                        {
                            string command = ffmpeg_path + " " + ff_base_args + " -i \"" + currentMediaPath + "\" " +
                                             ff_filter_args + " " + ff_video_args + " " + ff_audio_args +
                                             " \"" + proxyPath + "\"";
                            RunFFmpeg(command);
                        }

                        processedCount++;
                        ReplaceVideo(videoEvent, proxyPath);
                    }
                }
            }
        }

        if (processedCount == 0)
        {
            MessageBox.Show("No selected videos to process. Highlight the clips on your timeline before running VEGAS-PROXY.");
        }
    }

    public static string GetFullPath(string fileName)
    {
        if (File.Exists(fileName))
            return Path.GetFullPath(fileName);

        foreach (var path in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
                return fullPath;
        }
        return null;
    }

    void ReplaceVideo(VideoEvent vidEvent, string newMediaPath)
    {
        Media newMedia = new Media(newMediaPath);
        if (newMedia == null)
        {
            if (CONSOLE_SHOW) Console.WriteLine("Error: Failed to load media from " + newMediaPath);
            return;
        }

        MediaStream newMediaStream = newMedia.GetVideoStreamByIndex(0);
        if (newMediaStream == null)
        {
            if (CONSOLE_SHOW) Console.WriteLine("Error: No valid video stream found in " + newMediaPath);
            return;
        }

        Timecode originalOffset = vidEvent.ActiveTake.Offset;
        Timecode eventLength = vidEvent.Length;

        vidEvent.Takes.Clear();

        Take newTake = vidEvent.AddTake(newMediaStream, true);
        if (newTake == null)
        {
            if (CONSOLE_SHOW) Console.WriteLine("Error: Failed to create a new take for " + newMediaPath);
            return;
        }

        newTake.Offset = originalOffset;
        vidEvent.Length = eventLength;

        if (CONSOLE_SHOW)
        {
            Console.WriteLine("Replaced media with: " + newMediaPath);
            Console.WriteLine("Preserved trim: Offset=" + originalOffset + ", Length=" + eventLength);
        }
    }

    void RunFFmpeg(string command)
    {
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = CONSOLE_SHOW ? "/c echo " + command + " & echo. & " + command + " || pause" : "/c " + command,
            WindowStyle = CONSOLE_SHOW ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
            CreateNoWindow = !CONSOLE_SHOW,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = true
        };

        using (Process process = new Process { StartInfo = processInfo })
        {
            process.Start();
            process.WaitForExit();
        }
    }
}
