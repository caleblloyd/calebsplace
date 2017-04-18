using App.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace App.Api.Models
{

    public class PixelFetcher
    {
        public const int Pixels = 1000;
        private const int BatchCount = 10;
        public const int PixelsInBatch = Pixels / BatchCount;
        private const int SlidingExpirationSeconds = 60;
        private const int FlushToDbSeconds = 1;
        private readonly int _batchNumber;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        protected PixelFetcher(int batchNumber)
        {
            _batchNumber = batchNumber;
            if (LazyFlushTask.Value == null)
                throw new InvalidOperationException("Flush task has failed");
        }

        protected async Task<Pixel[,]> FetchAsync(IMemoryCache cache)
        {
            await _lock.WaitAsync();
            try
            {
                return await cache.GetOrCreateAsync(this, async entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromSeconds(SlidingExpirationSeconds);
                    ICollection<Pixel> existingPixels;
                    await DbLock.WaitAsync();
                    try
                    {
                        existingPixels = await Db.Pixels
                            .Where(m => m.X >= _batchNumber * PixelsInBatch && m.X < (_batchNumber + 1) * PixelsInBatch)
                            .OrderBy(m => m.X).ThenBy(m => m.Y)
                            .ToListAsync();
                    }
                    finally
                    {
                        DbLock.Release();
                    }
                    var pixels = new Pixel[PixelsInBatch, Pixels];
                    foreach (var existingPixel in existingPixels)
                        pixels[existingPixel.X - existingPixel.X / PixelsInBatch * PixelsInBatch, existingPixel.Y] = existingPixel;
                    for (var x = 0; x < PixelsInBatch; x++)
                        for (var y = 0; y < Pixels; y++)
                            if (pixels[x, y] == null)
                                pixels[x, y] = new Pixel(_batchNumber * PixelsInBatch + x, y);
                    return pixels;
                });
            }
            finally
            {
                _lock.Release();
            }
        }

        private static readonly AppDb Db = new AppDb();

        private static readonly SemaphoreSlim DbLock = new SemaphoreSlim(1);

        private static readonly Lazy<PixelFetcher[]> LazyPixelFetchers = new Lazy<PixelFetcher[]>(() =>
        {
            var pixelFetcher = new PixelFetcher[BatchCount];
            for (var i = 0; i < BatchCount; i++)
                pixelFetcher[i] = new PixelFetcher(i);
            return pixelFetcher;
        });

        private static readonly Lazy<Task> LazyFlushTask = new Lazy<Task>(() =>
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    var task = Task.Delay(TimeSpan.FromSeconds(FlushToDbSeconds));
                    try
                    {
                        await DbLock.WaitAsync();
                        try
                        {
                            await Db.SaveChangesAsync();
                        }
                        finally
                        {
                            DbLock.Release();
                        }
                    }
                    catch
                    {
                        // do nothing; we'll try to flush again
                    }
                    await task;
                }
            });
        });

    public static async Task<Pixel[,]> FetchBatchAsync(IMemoryCache cache, int batchNumber)
        {
            var fetcher = LazyPixelFetchers.Value[batchNumber];
            return await fetcher.FetchAsync(cache);
        }

        public static async Task UpdatePixelAsync(IMemoryCache cache, int x, int y, byte[] color)
        {
            var fetcher = LazyPixelFetchers.Value[x / PixelsInBatch];
            var pixel = (await fetcher.FetchAsync(cache))[x - x / PixelsInBatch * PixelsInBatch, y];
            await pixel.Lock.WaitAsync();
            try
            {
                pixel.Color = color;

                if (!pixel.Added)
                    Db.Pixels.Add(pixel);
                pixel.MarkTouched();
            }
            finally
            {
                pixel.Lock.Release();
            }
        }

    }
    
}

