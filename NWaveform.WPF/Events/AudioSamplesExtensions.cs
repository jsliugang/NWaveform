﻿using System;

namespace NWaveform.Events
{
    public static class AudioSamplesExtensions
    {
        public static PointsReceivedEvent ToPoints(this PeaksReceivedEvent e, double duration, double width, double height)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (e.Peaks == null || e.Peaks.Length == 0) throw new ArgumentException("Must not be null or empty", nameof(e.Peaks));

            var sx = width / duration;
            var sy = height / 2.0d;

            var x0 = (int)(sx * e.Start);
            var x1 = (int)(sx * e.End);
            var n = x1 - x0;

            var leftPoints = new int[n];
            var rightPoints = new int[n];

            var st = e.Peaks.Length / (e.End - e.Start);

            for (var i = 0; i < n; i++)
            {
                var x = x0 + i;
                var j = (int)(st * x / sx - e.Start * st);
                j = Math.Max(0, Math.Min(j, e.Peaks.Length - 1));
                var yl = (int)(sy * (1 - e.Peaks[j].Max));
                var yr = (int)(sy * (1 - e.Peaks[j].Min));
                leftPoints[i] = yl;
                rightPoints[i] = yr;
            }

            return new PointsReceivedEvent(e.Source, x0, leftPoints, rightPoints);
        }
    }
}