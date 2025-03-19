// https://github.com/Vramuser/ffmveg-revamp <- download to the script.
using System;
using System.IO;
using ScriptPortal.Vegas;
using System.Windows.Forms;

class EntryPoint
{
    public void FromVegas(Vegas vegas)
    {
        string proxy_file_prefix = "PROXY-"; // Proxy prefix
        int proxyCount = 0;

        foreach (Track track in vegas.Project.Tracks)
        {
            if (!track.IsVideo()) continue; // Checks if its a Video

            foreach (TrackEvent trackEvent in track.Events)
            {
                if (!trackEvent.IsVideo()) continue; // Will add the video to a list to check

                VideoEvent videoEvent = (VideoEvent)trackEvent;
                Take activeTake = videoEvent.ActiveTake;

                // Ensure there is an active take
                if (activeTake == null || activeTake.Media == null) continue;

                string currentMediaPath = activeTake.MediaPath;

                // Validate media path
                if (string.IsNullOrEmpty(currentMediaPath) || !File.Exists(currentMediaPath)) continue;

                string filename;
                try
                {
                    filename = Path.GetFileName(currentMediaPath);
                }
                catch
                {
                    continue;
                }

                // Check if the file has the "PROXY-" prefix
                if (filename.StartsWith(proxy_file_prefix, StringComparison.OrdinalIgnoreCase))
                {
                    trackEvent.Selected = true;
                    proxyCount++;
                }
                else
                {
                    trackEvent.Selected = false;
                }
            }
        }

        // Box shows with number of videos with the prefix
        string message = proxyCount > 0
            ? string.Format("{0} proxy clips have been selected.", proxyCount)
            : "No proxy clips found!";

        MessageBox.Show(message, "Proxy Highlighter");
    }
}
