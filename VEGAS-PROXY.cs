﻿using System;
using System.IO;
using ScriptPortal.Vegas;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;

class EntryPoint
{
    public void FromVegas(Vegas vegas)
    {
        string global_output_folder = "";
        bool only_selected = false;
        bool hide_proxy_files = true;

        string proxy_file_prefix = "PROXY-";
        string ffmpeg_path = "ffmpeg.exe";
        string ff_base_args = "-loglevel error -stats";
        string ff_filter_args = "-vf fps=60,scale=960:540:flags=neighbor";
        //string ff_video_args = "-vcodec h264_nvenc -profile:v high -preset fast -qmin 24 -qmax 24 -c:a copy";
        string ff_video_args = "-c:v libx264 -tune fastdecode -preset veryfast -g 60 -x264-params bframes=0 -crf 25 -forced-idr 1 -strict -2 -maxrate 100M -bufsize 10M";
        string ff_audio_args = "-an";

        if (!File.Exists(ffmpeg_path) && (GetFullPath(ffmpeg_path) == null))
        {
            // if you're using it from PATH be sure to add .exe at  the end of the string
            MessageBox.Show(
                "VEGAS-PROXY: Provided ffmpeg.exe path does not exist\n\nIs it not installed or not in PATH?\n\n See https://ctt.cx/ffmpeg for install instructions");
            return;
        }
        if (global_output_folder != "" && !Directory.Exists(global_output_folder))
        {
            MessageBox.Show("Provided global output folder `" + global_output_folder + "` does not exist, creating it");
            Directory.CreateDirectory(global_output_folder);
        }
        int ran = 0;
        foreach (Track track in vegas.Project.Tracks)
        {
            if (track.IsValid() && track.IsVideo() && (only_selected || track.Selected))
            {

                foreach (TrackEvent trackEvent in track.Events)
                {

                    if (trackEvent.IsVideo())
                    {

                        VideoEvent videoEvent = (VideoEvent)trackEvent;
                        string currentMediaPath = Path.GetFullPath(videoEvent.ActiveTake.MediaPath);
                        string Filename = Path.GetFileName(currentMediaPath);
                        string DirectoryPath; 
                        if (global_output_folder == ""){
                            DirectoryPath = Path.GetDirectoryName(currentMediaPath);
                        }
                        else {
                            DirectoryPath = global_output_folder;
                        }

                        if (!File.Exists(currentMediaPath))
                        {
                            continue;
                        }

                        if (Filename.StartsWith(proxy_file_prefix)) // we are already dealing with a proxied file
                        {
                            string oldPath =
                                DirectoryPath + Path.DirectorySeparatorChar + Regex.Replace(Filename, ("^" + proxy_file_prefix), "");

                            ran += 1;
                            ReplaceVideo(videoEvent, oldPath);
                        }
                        else
                        {

                            string outPath = DirectoryPath + Path.DirectorySeparatorChar + proxy_file_prefix + Filename;

                            if (File.Exists(outPath))
                            {
                                if ((new System.IO.FileInfo(outPath).Length) == 0)
                                {
                                    MessageBox.Show("Deleting empty corrupted file: " + outPath);
                                    // File.Delete(outPath)
                                }
                            }
                            else
                            {
                                // you can't imagine how scuffed the whatever c# version vegas runs this with
                                string command = ffmpeg_path + " " + ff_base_args + " -i \"" + currentMediaPath + "\" " +
                                                 " " + ff_filter_args + " " + ff_video_args + " " + ff_audio_args +
                                                 " \"" + outPath + "\"";

                                SmoothieInit(command);
                            }

                            ran += 1;
                            ReplaceVideo(videoEvent, outPath);
                            if (hide_proxy_files){
                                HideFile(outPath);
                            }
                        }
                    }
                }
            }
        }
        if (ran == 0)
        {
            MessageBox.Show("Selected videos on your timeline before running VEGAS-PROXY");
        }
    }
    // http://stackoverflow.com/questions/3855956/ddg#3856090
    public static string GetFullPath(string fileName)
    {
        if (File.Exists(fileName))
            return Path.GetFullPath(fileName);

        var values = Environment.GetEnvironmentVariable("PATH");
        foreach (var path in values.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
                return fullPath;
        }
        return null;
    }

    void ReplaceVideo(VideoEvent vidEvent, string NewVid)
    {
        Media newMedia = new Media(NewVid);
        MediaStream newMediaStream = newMedia.GetVideoStreamByIndex(0);

        // Check if there's an existing take
        if (vidEvent.Takes.Count > 0)
        {
            // Remove existing take
            vidEvent.Takes.RemoveAt(0);
        }

        // Add new take
        vidEvent.AddTake(newMediaStream, true);
        // Media newMedia = new Media(NewVid);
        // MediaStream newMediaStream = newMedia.GetVideoStreamByIndex(0);
        // vidEvent.AddTake(newMediaStream, true);
    }

    void HideFile(string Filepath)
    {

        FileAttributes attributes = File.GetAttributes(Filepath);
        File.SetAttributes(Filepath, File.GetAttributes(Filepath) | FileAttributes.Hidden);
    }

    void SmoothieInit(string Args)
    {
        Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = "/c echo " + Args + " & echo. & " + Args + " || pause";
        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        process.StartInfo.CreateNoWindow = false;
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            return;
        }
    }
}