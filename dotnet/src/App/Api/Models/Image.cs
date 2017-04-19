﻿using System;
using System.Collections.Generic;
using System.Threading;
using App.Api.Repositories;

namespace App.Api.Models
{
    public class Image
    {
        public readonly SortedDictionary<ulong, Pixel> Sorted = new SortedDictionary<ulong, Pixel>();
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private readonly Pixel[,] _pixels;

        public Image()
        {
            _pixels = new Pixel[PixelFetcher.Pixels, PixelFetcher.Pixels];
            for (var x = 0; x < PixelFetcher.Pixels; x++)
            {
                for (var y = 0; y < PixelFetcher.Pixels; y++)
                {
                    _pixels[x, y] = new Pixel(x, y);
                    Sorted.Add(_pixels[x, y].Key, _pixels[x, y]);
                }
            }
        }

        public void UpdateBatch(IEnumerable<Pixel> pixels)
        {
            _rwLock.EnterWriteLock();
            try
            {
                foreach (var pixel in pixels)
                {
                    Sorted.Remove(_pixels[pixel.X, pixel.Y].Key);
                    _pixels[pixel.X, pixel.Y].Copy(pixel);
                    Sorted.Add(_pixels[pixel.X, pixel.Y].Key, _pixels[pixel.X, pixel.Y]);
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public PixelsUpdatedSince UpdatedSecond(DateTime since)
        {
            var to = since + TimeSpan.FromSeconds(1);
            var pixels = new List<Pixel>();
            _rwLock.EnterReadLock();
            try
            {
                foreach (var kvp in Sorted)
                {
                    var pixel = kvp.Value;
                    if (pixel.Updated > since)
                    {
                        if (pixel.Updated <= to)
                            pixels.Add(pixel);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return new PixelsUpdatedSince
            {
                LastUpdated = to,
                Pixels = pixels
            };
        }

    }
}