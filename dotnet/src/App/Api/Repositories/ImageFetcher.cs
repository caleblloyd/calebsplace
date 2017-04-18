using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using App.Api.Models;

namespace App.Api.Repositories
{
    public class ImageFetcher
    {
        private const int FetchImageSeconds = 1;
        private readonly int _batchNumber;
        private DateTime _lastUpdated = new DateTime(0, DateTimeKind.Utc);

        protected ImageFetcher(int batchNumber)
        {
            _batchNumber = batchNumber;
        }

        protected async Task FetchAsync()
        {
            var response = await HttpClient.GetAsync($"http://localhost:5000/api/pixels/{_batchNumber}?since={_lastUpdated:o}");
            var pixelsUpdatedSince = PixelsUpdatedSince.FromStream(await response.Content.ReadAsStreamAsync());
            _lastUpdated = pixelsUpdatedSince.LastUpdated;
            Image.UpdateBatch(pixelsUpdatedSince.Pixels);
        }
        
        public static HttpClient HttpClient = new HttpClient();
        public static Image Image = new Image();
        
        private static readonly Lazy<ImageFetcher[]> LazyImageFetchers = new Lazy<ImageFetcher[]>(() =>
        {
            var imageFetcher = new ImageFetcher[PixelFetcher.BatchCount];
            for (var i = 0; i < PixelFetcher.BatchCount; i++)
                imageFetcher[i] = new ImageFetcher(i);
            return imageFetcher;
        });

        public static Task Fetcher = Task.Run(async () => {
            while (true)
            {
                var task = Task.Delay(TimeSpan.FromSeconds(FetchImageSeconds));
                try
                {
                    var tasks = new List<Task>();
                    for (var i = 0; i < PixelFetcher.BatchCount; i++)
                        tasks.Add(LazyImageFetchers.Value[i].FetchAsync());
                    await Task.WhenAll(tasks);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.StackTrace);
                    // continue; we'll try to fetch again
                }
                await task;
            }
        });
    }
}