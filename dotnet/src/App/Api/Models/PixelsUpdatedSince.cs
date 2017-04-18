using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace App.Api.Models
{
    public class PixelsUpdatedSince
    {
        private DateTime _lastUpdated;
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => _lastUpdated = new DateTime(value.Ticks, DateTimeKind.Utc);
        }

        public IEnumerable<Pixel> Pixels;

        public int Count;

        public MemoryStream GetMemoryStream(string seperator = "\n")
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 512, true))
            {
                writer.Write($"{LastUpdated:o}");
                foreach (var pixel in Pixels)
                {
                    writer.Write($"{seperator}{pixel.X},{pixel.Y},{pixel.ColorStr},{pixel.Updated:o}");
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static PixelsUpdatedSince FromStream(Stream stream)
        {
            var pixelsUpdatedSince = new PixelsUpdatedSince();
            var pixels = new List<Pixel>();
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                pixelsUpdatedSince.LastUpdated = DateTime.Parse(reader.ReadLine());
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;
                    var components = line.Split(',');
                    var pixel = new Pixel(int.Parse(components[0]), int.Parse(components[1]))
                    {
                        ColorStr = components[2],
                        Updated = DateTime.Parse(components[3])
                    };
                    pixels.Add(pixel);
                }
            }
            pixelsUpdatedSince.Pixels = pixels;
            pixelsUpdatedSince.Count = pixels.Count;
            return pixelsUpdatedSince;
        }
    }
}
