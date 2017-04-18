using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using App.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace App.Api.Models{

    public class Pixel
	{
	    public Pixel()
	    {
	    }

	    public Pixel(int x, int y)
	    {
	        X = x;
	        Y = y;
	    }

		public int X { get; set; }

	    public int Y { get; set; }


	    private byte[] _color = {0xFF, 0xFF, 0xFF};
	    [JsonIgnore]
	    public byte[] Color
	    {
	        get => _color;
	        set
	        {
	            _color = value;
	            Updated = DateTime.UtcNow;
	            if (Created == default(DateTime))
	                Created = Updated;
	        }
	    }

	    [NotMapped]
	    public string ColorStr
	    {
	        get => BitConverter.ToString(Color).Replace("-", string.Empty);
	        set => Color = Enumerable.Range(0, value.Length / 2)
	            .Select(i => Convert.ToByte(value.Substring(i * 2, 2), 16))
	            .ToArray();
	    }

	    [JsonIgnore]
	    public DateTime Created { get; private set; }
	    
	    public DateTime Updated { get; set; }

	    [JsonIgnore]
	    [NotMapped]
	    public bool IsNew => Created == default(DateTime);

	    [JsonIgnore]
	    [NotMapped]
	    public ulong Key => ~(((ulong) Updated.Ticks & 0xFFFFFFFFFFF00000) + (ulong) X * PixelFetcher.Pixels + (ulong) Y);

	    [JsonIgnore]
	    public readonly SemaphoreSlim Lock = new SemaphoreSlim(1);

	    public void Copy(Pixel other)
	    {
	        ColorStr = other.ColorStr;
	        Created = other.Created;
	        Updated = other.Updated;
	    }
	}

	public static class PixelMeta
	{
		public static void OnModelCreating(ModelBuilder modelBuilder){

            modelBuilder.Entity<Pixel>(entity =>
            {
                entity.HasKey(m => new {m.X, m.Y});
            });
        }
	}
    
}

