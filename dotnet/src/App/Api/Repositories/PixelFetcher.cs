using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Api.Models;
using App.Db;
using Microsoft.EntityFrameworkCore;

namespace App.Api.Repositories
{

    public class PixelFetcher
    {
        public const int Pixels = 1000;
        public const int BatchCount = 10;
        public const int PixelsInBatch = Pixels / BatchCount;

        private const int FlushToDbSeconds = 1;
        private readonly int _batchNumber;

        protected PixelFetcher(int batchNumber)
        {
            _batchNumber = batchNumber;
            LazyPixelBatchPairReset();
        }

        protected Lazy<Task<PixelBatchPair>> LazyPixelBatchPair;

        protected void LazyPixelBatchPairReset()
        {
            LazyPixelBatchPair = new Lazy<Task<PixelBatchPair>>(async () =>
            {
                ICollection<Pixel> existingPixels;
                await DbLock.WaitAsync();
                try
                {
                    existingPixels = await Db.Pixels
                        .Where(m => m.X >= _batchNumber * PixelsInBatch && m.X < (_batchNumber + 1) * PixelsInBatch)
                        .OrderBy(m => m.X)
                        .ThenBy(m => m.Y)
                        .ToListAsync();
                }
                finally
                {
                    DbLock.Release();
                }
                var memoryBatch = new PixelBatch(_batchNumber, existingPixels, true);
                var dbBatch = new PixelBatch(_batchNumber, existingPixels, false);
                return new PixelBatchPair(memoryBatch, dbBatch);
            });
        }

        private static readonly AppDb Db = new AppDb();
        private static readonly SemaphoreSlim DbLock = new SemaphoreSlim(1);
        private static readonly ConcurrentQueue<PixelUpdate> Updates = new ConcurrentQueue<PixelUpdate>();

        private static readonly Lazy<PixelFetcher[]> LazyPixelFetchers = new Lazy<PixelFetcher[]>(() =>
        {
            var pixelFetcher = new PixelFetcher[BatchCount];
            for (var i = 0; i < BatchCount; i++)
                pixelFetcher[i] = new PixelFetcher(i);
            return pixelFetcher;
        });

        public static async Task<PixelsUpdatedSince> UpdatedSinceAsync(int batchNumber, DateTime since)
        {
            var fetcher = LazyPixelFetchers.Value[batchNumber];
            var batchPair = await fetcher.LazyPixelBatchPair.Value;
            return await batchPair.MemoryBatch.UpdatedSinceAsync(since);
        }

        public static async Task UpdatePixelAsync(int x, int y, byte[] color)
        {
            var fetcher = LazyPixelFetchers.Value[x / PixelsInBatch];
            var batchPair = await fetcher.LazyPixelBatchPair.Value;
            var updatedPixel = await batchPair.MemoryBatch.UpdateAsync(x, y, color);
            Updates.Enqueue(new PixelUpdate(updatedPixel, batchPair.DbBatch.Find(x, y)));
        }

        public static Task Flusher = Task.Run(async () => {
            while (true)
            {
                var task = Task.Delay(TimeSpan.FromSeconds(FlushToDbSeconds));
                try
                {
                    await DbLock.WaitAsync();
                    try
                    {
                        PixelUpdate update;
                        while (Updates.TryDequeue(out update))
                        {
                            var isNew = update.To.IsNew;
                            await update.From.Lock.WaitAsync();
                            try
                            {
                                update.To.Copy(update.From);
                            }
                            finally
                            {
                                update.From.Lock.Release();
                            }
                            if (isNew)
                                Db.Pixels.Add(update.To);

                        }
                        await Db.SaveChangesAsync();
                    }
                    finally
                    {
                        DbLock.Release();
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.StackTrace);
                    // continue; we'll try to flush again
                }
                await task;
            }
        });

    }

}
