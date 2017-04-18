using App.Api.Models;
using App.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;

namespace App.Api.Models
{

    public class PixelFetcher
	{	
        private const int BatchCount = 10;
        private const int BatchMultiplier = 100;
        private const int SlidingExpirationSeconds = 3;
        private int _batchNumber;
        private SemaphoreSlim Lock = new SemaphoreSlim(1);

        protected PixelFetcher(int batchNumber)
        {
            _batchNumber = batchNumber;
        }

        protected async Task<ICollection<Pixel>> FetchAsync(IMemoryCache cache, AppDb db)
        {
            await Lock.WaitAsync();
            try
            {
                return await cache.GetOrCreateAsync(this, async entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromSeconds(SlidingExpirationSeconds);
                    return await db.Pixels.OrderByDescending(m => new {m.X, m.Y}).ToListAsync(m => m >= BatchCount * BatchMultiplier && m < (BatchCount + 1) * BatchMultiplier);
                });
            }
            finally
            {
                Lock.Release();
            }
        }

        private static SemaphoreSlim SLock = new SemaphoreSlim(1);
        private static Lazy<PixelFetcher[]> LazyPixelFetchers = new Lazy<PixelFetcher[]>(() => {
            var pixelFetcher = new PixelFetcher[BatchCount];
            for (var i=0; i<BatchCount; i++)
                pixelFetcher[i] = new PixelFetcher(i);
            return pixelFetcher;
        });

        public static async Task<ICollection<Pixel>> FetchBatchAsync(IMemoryCache cache, AppDb db, int batchNumber)
        {
            var fetcher = LazyPixelFetchers.Value[batchNumber];
            return await fetcher.FetchAsync(cache, db);
        }

    }
    
}
