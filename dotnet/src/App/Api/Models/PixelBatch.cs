using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using App.Api.Repositories;

namespace App.Api.Models
{
    public class PixelBatch
    {
        public readonly SortedDictionary<ulong, Pixel> Sorted = new SortedDictionary<ulong, Pixel>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private readonly int _batchNumber;
        private readonly Pixel[,] _pixels;

        private static int CoordinateToBatch(int x)
        {
            return x - x / PixelFetcher.PixelsInBatch * PixelFetcher.PixelsInBatch;
        }

        private int BatchToCoordinate(int x)
        {
            return _batchNumber * PixelFetcher.PixelsInBatch + x;
        }

        public PixelBatch(int batchNumber, IEnumerable<Pixel> existingPixels, bool copy)
        {
            _batchNumber = batchNumber;
            _pixels = new Pixel[PixelFetcher.PixelsInBatch, PixelFetcher.Pixels];

            foreach (var existingPixel in existingPixels)
            {
                if (copy)
                {
                    var pixel = new Pixel(existingPixel.X, existingPixel.Y);
                    pixel.Copy(existingPixel);
                    _pixels[CoordinateToBatch(existingPixel.X), existingPixel.Y] = pixel;
                }
                else
                {
                    _pixels[CoordinateToBatch(existingPixel.X), existingPixel.Y] = existingPixel;
                }
            }

            for (var x = 0; x < PixelFetcher.PixelsInBatch; x++)
            {
                for (var y = 0; y < PixelFetcher.Pixels; y++)
                {

                    if (_pixels[x, y] == null)
                        _pixels[x, y] = new Pixel(BatchToCoordinate(x), y);
                    Sorted.Add(_pixels[x, y].Key, _pixels[x, y]);
                }
            }
        }

        public Pixel Find(int x, int y)
        {
            return _pixels[x - x / PixelFetcher.PixelsInBatch * PixelFetcher.PixelsInBatch, y];
        }

        public async Task<Pixel> UpdateAsync(int x, int y, byte[] color)
        {
            await _lock.WaitAsync();
            try
            {
                var pixel = Find(x, y);
                await pixel.Lock.WaitAsync();
                try
                {
                    Sorted.Remove(pixel.Key);
                    pixel.Color = color;
                    Sorted.Add(pixel.Key, pixel);
                    return pixel;
                }
                finally
                {
                    pixel.Lock.Release();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<PixelsUpdatedSince> UpdatedSinceAsync(DateTime since)
        {
            var pixels = new List<Pixel>();
            var lastUpdated = default(DateTime);
            await _lock.WaitAsync();
            try
            {
                foreach (var kvp in Sorted)
                {
                    var pixel = kvp.Value;
                    if (pixel.Updated > lastUpdated)
                        lastUpdated = pixel.Updated;
                    if (since == default(DateTime) || pixel.Updated > since)
                        pixels.Add(pixel);
                    else
                        break;
                }
            }
            finally
            {
                _lock.Release();
            }
            return new PixelsUpdatedSince
            {
                LastUpdated = lastUpdated,
                Pixels = pixels
            };
        }

//        public async Task BatchUpdateAsync(IEnumerable<Pixel> batch)
//        {
//            await Lock.WaitAsync();
//            try
//            {
//                foreach (var newPixel in batch)
//                {
//                    newPixel.MarkAdded();
//                    var oldPixel = _pixels[CoordinateToBatch(newPixel.X), newPixel.Y];
//                    Sorted.Remove(oldPixel.Key);
//                }
//                try
//                {
//                    Sorted.Remove(pixel.Key);
//                    pixel.Color = color;
//                    pixel.MarkTouched();
//                    Sorted.Add(pixel.Key, pixel);
//                    return pixel;
//                }
//                finally
//                {
//                    pixel.Lock.Release();
//                }
//            }
//            finally
//            {
//                Lock.Release();
//            }
//        }

    }
}