using System;
using System.Linq;
using FluentAssertions;
using NAudio.Wave;
using NEdifis.Attributes;
using NUnit.Framework;

namespace NWaveform.NAudio
{
    [TestFixtureFor(typeof(BufferedWaveStream))]
    // ReSharper disable InconsistentNaming
    internal class BufferedWaveStream_Should
    {
        [Test]
        public void Clamp_positions()
        {
            var waveFormat = new WaveFormat(8000, 1);
            var duration = TimeSpan.FromSeconds(1);

            var sut = new BufferedWaveStream(waveFormat, duration);
            sut.BufferDuration.Should().Be(duration);
            sut.Length.Should().Be(sut.BufferLength);
            sut.BufferLength.Should().Be((int)(waveFormat.AverageBytesPerSecond * duration.TotalSeconds));

            sut.Position.Should().Be(0);
            sut.Position = sut.BufferLength + 1;
            sut.Position.Should().Be(sut.BufferLength);

            sut.Position = -1;
            sut.Position.Should().Be(0);
        }

        [Test]
        public void Preserve_after_wrap_around()
        {
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(8000, 1);
            var bufferSize = TimeSpan.FromSeconds(1);
            using (var sut = new BufferedWaveStream(waveFormat, bufferSize))
            {
                sut.PreserveAfterWrapAround.Should()
                    .Be(TimeSpan.Zero, "default preserve should be zero (no preservation)");

                sut.Invoking(x => x.PreserveAfterWrapAround = bufferSize)
                    .Should().Throw<ArgumentOutOfRangeException>("preserve must be less than buffer size");

                // 1. no wrap around
                var expectedBytes = waveFormat.Generate(bufferSize);
                sut.AddSamples(expectedBytes, 0, expectedBytes.Length);

                var actualBytes = new byte[expectedBytes.Length];
                var numbytes = sut.Read(actualBytes, 0, actualBytes.Length);
                numbytes.Should().Be(actualBytes.Length);
                actualBytes.Should().Equal(expectedBytes, "no wrap around");

                // 2. wrap around: check data/position preserved after wrap around
                // position half-way between end & preserve
                sut.PreserveAfterWrapAround = TimeSpan.FromSeconds(bufferSize.TotalSeconds * 0.75);
                sut.ClearBuffer();

                var skippedTime = sut.SkippedDuration;
                var time = TimeSpan.FromSeconds(0.5 * (skippedTime.TotalSeconds + sut.BufferDuration.TotalSeconds));
                sut.CurrentTime = time;

                using (var monitor = sut.Monitor())
                {
                    sut.AddSamples(expectedBytes, 0, expectedBytes.Length - 1);
                    sut.WritePosition.Should().Be(expectedBytes.Length - 1);
                    monitor.Should().NotRaise(nameof(sut.WrappedAround));
                }

                using (var monitor = sut.Monitor())
                {
                    sut.AddSamples(expectedBytes, expectedBytes.Length - 1, 1);
                    monitor.Should().Raise(nameof(sut.WrappedAround));
                    sut.CurrentWriteTime.Should().Be(sut.PreserveAfterWrapAround);
                    sut.CurrentTime.Should().Be(time - skippedTime, "position should be wrapped");
                }

                Array.Resize(ref actualBytes, sut.PreservedBytes);
                sut.Position = 0;
                sut.Read(actualBytes, 0, actualBytes.Length);
                sut.Position.Should().Be(actualBytes.Length);

                expectedBytes = expectedBytes.Skip(sut.SkippedBytes).ToArray();
                actualBytes.Should().Equal(expectedBytes);
            }
        }
    }
}
