using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageSharp;
using ImageSharp.PixelFormats;

namespace App.Api.Repositories
{
    public class ImageDraw
    {
        private const int ImageDrawWorkers = 2;
        private const int DrawImageSeconds = 1;
        private MemoryStream _stream;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        public ImageDraw()
        {
            DrawImage();
        }

        protected void DrawImage()
        {
            var stream = new MemoryStream();
            var pixels = ImageFetcher.Image.Pixels;
            using (var image = new Image(PixelFetcher.Pixels, PixelFetcher.Pixels))
            {
                using (var imagePixels = image.Lock())
                {
                    for (var x = 0; x < PixelFetcher.Pixels; x++)
                    {
                        for (var y = 0; y < PixelFetcher.Pixels; y++)
                        {
                            var color = pixels[x, y].Color;
                            var imageColor = new Rgba32();
                            imageColor.PackFromBytes(color[0], color[1], color[2], 0xFF);
                            imagePixels[x, y] = imageColor;
                        }
                    }
                }
                image.SaveAsPng(stream);
            }
            stream.Seek(0, SeekOrigin.Begin);
            _stream = stream;
        }

        public async Task<Stream> GetMemoryStreamAsync()
        {
            var stream = new MemoryStream();
            await _lock.WaitAsync();
            try
            {
                await _stream.CopyToAsync(stream);
                _stream.Seek(0, SeekOrigin.Begin);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
            finally
            {
                _lock.Release();
            }
        }

        public static ImageDraw Draw => LazyImageDrawWorkers.Value[ActiveWorker % ImageDrawWorkers];

        private static ulong ActiveWorker;

        private static readonly Lazy<ImageDraw[]> LazyImageDrawWorkers = new Lazy<ImageDraw[]>(() =>
        {
            var imageDraw = new ImageDraw[ImageDrawWorkers];
            for (var i = 0; i < ImageDrawWorkers; i++)
                imageDraw[i] = new ImageDraw();
            return imageDraw;
        });

        public static Task DrawWorkers = Task.Run(async () => {
            while (true)
            {
                var task = Task.Delay(TimeSpan.FromSeconds(DrawImageSeconds));
                try
                {
                    LazyImageDrawWorkers.Value[(ActiveWorker + 1) % ImageDrawWorkers].DrawImage();
                    ActiveWorker++;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.StackTrace);
                    // continue; we'll try to draw again
                }
                await task;
            }
        });
    }
}
