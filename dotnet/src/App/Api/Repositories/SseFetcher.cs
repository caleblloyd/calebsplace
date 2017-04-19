using System;
using System.Collections.Generic;
using App.Api.Models;

namespace App.Api.Repositories
{
    public class SseFetcher
    {
        private const int SseFetcherCount = 30;
        private const int SseLeadTime = 5;
        protected Lazy<PixelsUpdatedSince> LazyPixelsUpdatedSince;
        private DateTime _lastResetUtc;
        private readonly object Lock = new object();

        protected void MaybeReset(DateTime dateTime)
        {
            if ((dateTime - _lastResetUtc).TotalSeconds >= SseFetcherCount)
            {
                lock (Lock)
                {
                    if ((dateTime - _lastResetUtc).TotalSeconds >= SseFetcherCount)
                    {
                        LazyPixelsUpdatedSince = new Lazy<PixelsUpdatedSince>(() => ImageFetcher.Image.UpdatedSecond(dateTime));
                        _lastResetUtc = dateTime;
                    }
                }
            }
        }

        private static readonly DateTime Start = DateTime.UtcNow - TimeSpan.FromSeconds(SseFetcherCount);
        
        private static readonly Lazy<SseFetcher[]> LazySseFetchers = new Lazy<SseFetcher[]>(() =>
        {
            var sseFetchers = new SseFetcher[SseFetcherCount];
            for (var i = 0; i < SseFetcherCount; i++)
                sseFetchers[i] = new SseFetcher();
            return sseFetchers;
        });

        public static List<PixelsUpdatedSince> UpdatedSinceList(DateTime since)
        {
            var now = DateTime.UtcNow;
            var seconds = (int) Math.Ceiling((now - since).TotalSeconds);
            if (seconds < 0 || seconds >= SseFetcherCount)
                throw new InvalidOperationException($"Update must be within the past {SseFetcherCount} seconds");

            var secondsFromStart = (long) (since - Start).TotalSeconds;
            var startAt = Start + TimeSpan.FromSeconds(secondsFromStart);
            var pixelsUpdatedSince = new List<PixelsUpdatedSince>();

            for (var i = 0; i < seconds - SseLeadTime; i++)
            {
                var iterationTime = startAt + TimeSpan.FromSeconds(i);
                var index = (long) (iterationTime - Start).TotalSeconds % SseFetcherCount;
                var sseFetcher = LazySseFetchers.Value[index];
                sseFetcher.MaybeReset(iterationTime);
                pixelsUpdatedSince.Add(sseFetcher.LazyPixelsUpdatedSince.Value);
            }
            return pixelsUpdatedSince;
        }
    }
}