using System;
using System.Collections.Generic;
using Autofac;
using Caliburn.Micro;
using NWaveform.NAudio;
using NWaveform.ViewModels;

namespace NWaveform.App
{
    internal class StreamingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<StreamingWaveProviderFactory>().AsImplementedInterfaces().SingleInstance();

            var channels = new Dictionary<Uri, dynamic>
            {
                // we have channel 1 & 2 with a time shift because they are in the past (10 hours and 5 mins)
                {
                    new Uri("channel://1/"), new
                    {
                        TimeSpan = TimeSpan.FromMinutes(5),
                        WrapAround = new TimeSpan(0, 4, 50),
                        TimeShift = TimeSpan.FromHours(-10)
                    }
                },
                {
                    new Uri("channel://2/"), new
                    {
                        TimeSpan = TimeSpan.FromSeconds(30),
                        WrapAround = TimeSpan.FromSeconds(29),
                        TimeShift = TimeSpan.FromMinutes(-5)
                    }
                },
                {
                    new Uri("channel://3/"), new
                    {
                        TimeSpan = TimeSpan.FromSeconds(33),
                        WrapAround = TimeSpan.FromSeconds(25),
                        TimeShift = TimeSpan.FromHours(0)
                    }
                },
                {
                    new Uri("channel://4/"), new
                    {
                        TimeSpan = TimeSpan.FromSeconds(5),
                        WrapAround = TimeSpan.FromSeconds(4),
                        TimeShift = TimeSpan.FromHours(0)
                    }
                }
            };

            foreach (var kvp in channels)
            {
                builder.Register(c => CreateChannel(c, kvp.Key, kvp.Value.TimeSpan, kvp.Value.WrapAround, kvp.Value.TimeShift))
                    .As<IStreamingAudioChannel>();
                builder.Register(c => CreateChannelViewModel(c, kvp.Key, kvp.Value.TimeSpan.TotalSeconds))
                    .As<IChannelViewModel>();
            }

            builder.RegisterType<Mp3StreamChannelFactory>()
                .WithParameter(new NamedParameter("bufferSize", TimeSpan.FromMinutes(2)))
                .As<IChannelFactory>()
                .SingleInstance();

            builder.RegisterType<SamplesHandlerPeakPublisher>().AsSelf().AutoActivate().SingleInstance();
            builder.RegisterType<PeakProvider>().As<IPeakProvider>();

            builder.RegisterType<MixerChannel>().As<IMixerChannel>();
            builder.RegisterType<ChannelMixer>().As<IChannelMixer>();
        }

        private static EndlessFileLoopChannel CreateChannel(IComponentContext c, Uri source, TimeSpan bufferSize,
            TimeSpan wrapAround, TimeSpan timeShift)
        {
            var events = c.Resolve<IEventAggregator>();
            const string fileName = @"Data\Pulp_Fiction_Jimmys_Coffee.mp3";
            return new EndlessFileLoopChannel(events, source, fileName, bufferSize, timeShift)
            { PreserveAfterWrapAround = wrapAround };
        }

        private static IChannelViewModel CreateChannelViewModel(IComponentContext c, Uri source, double duration)
        {
            var waveform = c.Resolve<IWaveformDisplayViewModel>();
            waveform.Source = source;
            waveform.Duration = duration;

            var mixer = c.Resolve<IMixerChannel>();
            mixer.Source = source;

            var channel = new ChannelViewModel(waveform, mixer);
            return channel;
        }
    }
}
