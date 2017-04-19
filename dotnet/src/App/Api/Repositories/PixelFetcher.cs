using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Api.Models;
using App.Db;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

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

        protected Lazy<Task<PixelBatch>> LazyPixelBatch;

        protected void LazyPixelBatchPairReset()
        {
            LazyPixelBatch = new Lazy<Task<PixelBatch>>(async () =>
            {
                using (var db = new AppDb())
                {
                    var existingPixels = await db.Pixels
                        .AsNoTracking()
                        .Where(m => m.X >= _batchNumber * PixelsInBatch && m.X < (_batchNumber + 1) * PixelsInBatch)
                        .OrderBy(m => m.X)
                        .ThenBy(m => m.Y)
                        .ToListAsync();
                    return new PixelBatch(_batchNumber, existingPixels);
                }
            });
        }

        private static readonly ConcurrentQueue<Pixel> Updates = new ConcurrentQueue<Pixel>();

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
            var batch = await fetcher.LazyPixelBatch.Value;
            return await batch.UpdatedSinceAsync(since);
        }

        public static async Task UpdatePixelAsync(int x, int y, byte[] color)
        {
            var fetcher = LazyPixelFetchers.Value[x / PixelsInBatch];
            var batch = await fetcher.LazyPixelBatch.Value;
            var updatedPixel = await batch.UpdateAsync(x, y, color);
            Updates.Enqueue(updatedPixel);
        }

        public static Task Flusher = Task.Run(async () => {
            while (true)
            {
                var task = Task.Delay(TimeSpan.FromSeconds(FlushToDbSeconds));
                try
                {
                    using (var db = new AppDb())
                    using (var cmd = db.Database.GetDbConnection().CreateCommand())
                    {
                        var sql = "INSERT INTO `Pixels` (`X`, `Y`, `Color`, `Created`, `Updated`) VALUES ";
                        var num = 0;
                        Pixel pixel;
                        while (Updates.TryDequeue(out pixel))
                        {
                            if (num > 0)
                                sql += ", ";
                            sql += $"(@x{num}, @y{num}, @color{num}, @created{num}, @updated{num})";
                            await pixel.Lock.WaitAsync();
                            try
                            {
                                cmd.Parameters.Add(new MySqlParameter
                                {
                                    ParameterName = $"@x{num}",
                                    Value = pixel.X
                                });
                                cmd.Parameters.Add(new MySqlParameter
                                {
                                    ParameterName = $"@y{num}",
                                    Value = pixel.Y
                                });
                                cmd.Parameters.Add(new MySqlParameter
                                {
                                    ParameterName = $"@color{num}",
                                    Value = pixel.Color
                                });
                                cmd.Parameters.Add(new MySqlParameter
                                {
                                    ParameterName = $"@created{num}",
                                    Value = pixel.Created
                                });
                                cmd.Parameters.Add(new MySqlParameter
                                {
                                    ParameterName = $"@updated{num}",
                                    Value = pixel.Updated
                                });
                            }
                            finally
                            {
                                pixel.Lock.Release();
                            }
                            num++;
                        }
                        if (num > 0)
                        {
                            sql += " ON DUPLICATE KEY UPDATE `Color` = VALUES(`Color`), `Updated` = VALUES(`Updated`)";
                            cmd.CommandText = sql;
                            await db.Database.OpenConnectionAsync();
                            await cmd.ExecuteNonQueryAsync();
                            db.Database.CloseConnection();
                        }
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
