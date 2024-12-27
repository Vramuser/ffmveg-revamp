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
            if (track.IsVideo())
            {
                foreach (TrackEvent trackEvent in track.Events)
                {
                    if (trackEvent.IsVideo())
                    {
                        VideoEvent videoEvent = (VideoEvent)trackEvent;
                        string currentMediaPath = videoEvent.ActiveTake.MediaPath;
                        string filename = Path.GetFileName(currentMediaPath);

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
            }
        }

        if (proxyCount > 0)
        {
            MessageBox.Show(proxyCount + " All clips with PROXY- prefix have been selected. ", "Proxy Highlighter");
        }
        else
        {
            MessageBox.Show("None where found! :D", "Proxy Highlighter");
        }
    }
}
