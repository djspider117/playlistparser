using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaylistParser
{
    public class TrackInfo
    {
        public TimeSpan Timestamp { get; set; }
        public string Artist { get; set; }
        public string TrackName { get; set; }
        public string Remix { get; set; }

        public string GetTrackWithRemix()
        {
            if (string.IsNullOrEmpty(Remix))
                return TrackName;

            return $"{TrackName} ({Remix})";
        }
    }

    public class TrackInfoCollection : ConcurrentBag<TrackInfo> { }

    public class Parser
    {
        public TrackInfoCollection Parse(string filePath)
        {
            var rv = new TrackInfoCollection();
            var lines = File.ReadAllLines(filePath);

            Parallel.ForEach(lines, line =>
            {
                var trackInfo = new TrackInfo();
                var tsEnding = line.IndexOf(']');

                if (tsEnding == -1)
                    return;

                var timestamp = line.Substring(1, tsEnding - 1);
                var remaining = line.Remove(0, tsEnding + 1);
                var split = remaining.Split('-');

                if (split[1].Contains('('))
                {
                    var q = split[1].IndexOf('(') + 1;
                    int q2 = split[1].IndexOf(')');
                    var remix = split[1].Substring(q, q2 - q);
                    trackInfo.Remix = remix.Trim();

                    remaining = split[1].Substring(0, q - 1).Trim();
                }
                else
                {
                    remaining = split[1].Trim();
                }

                trackInfo.TrackName = remaining;
                trackInfo.Artist = split[0].Trim();
                try
                {
                    trackInfo.Timestamp = TimeSpan.Parse(timestamp);
                    if (trackInfo.Timestamp.Seconds == 0 && timestamp.Count(x => x == ':') == 1)
                        trackInfo.Timestamp = new TimeSpan(0, trackInfo.Timestamp.Hours, trackInfo.Timestamp.Minutes);
                }
                catch (OverflowException)
                {
                    var tss = timestamp.Split(':');
                    var hours = 0;
                    var minutes = int.Parse(tss[0]);
                    var sec = int.Parse(tss[1]);
                    if (minutes > 60)
                    {
                        hours = minutes / 60;
                        minutes %= hours;
                    }

                    trackInfo.Timestamp = new TimeSpan(hours, minutes, sec);
                }

                if (trackInfo != null)
                    rv.Add(trackInfo);
            });

            return rv;
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser();
            var trackInfos = parser.Parse("playlist.txt").OrderBy(x => x.Timestamp);

            var fps = 24;
            var times = new List<int>();
            var timesLines = File.ReadAllLines("times.txt");
            foreach (var x in timesLines)
            {
                var q = x.Split(':');
                var m = int.Parse(q[0]);
                var s = int.Parse(q[1]);
                var f = int.Parse(q[2]);

                f += s * fps + m * 60 * fps;
                times.Add(f);
            }

            var random = new Random();

            var sb = new StringBuilder();
            sb.AppendLine("Timestamp, Second, Artist, Song, VideoFrame");
            foreach (var x in trackInfos)
            {
                var frame = times[random.Next(times.Count)];
                sb.AppendLine($"[{x.Timestamp.Hours}:{x.Timestamp.Minutes}:{x.Timestamp.Seconds}], {x.Timestamp.TotalSeconds}, \"{x.Artist}\", \"{x.GetTrackWithRemix()}\", {frame}");
            }

            File.WriteAllText("yearmix2023.csv", sb.ToString());

            //int i = 1;
            //foreach (var x in trackInfos)
            //{
            //    Console.WriteLine($"------ Track {i++} ------");
            //    Console.WriteLine($"Timestamp: {x.Timestamp}");
            //    Console.WriteLine($"Artist: {x.Artist}");
            //    Console.WriteLine($"Track Name: {x.TrackName} {x.Remix}");
            //    if (x.Remix != null)
            //        Console.WriteLine($"Remix: {x.Remix}");
            //}
            //Console.Read();
        }
    }
}
