using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
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

	    [JsonIgnore]
	    public byte[] Color { get; set; } = {0xFF, 0xFF, 0xFF};

	    [NotMapped]
	    public string ColorStr => BitConverter.ToString(Color).Replace("-", string.Empty);

	    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	    [JsonIgnore]
	    public DateTime Created { get; set; }

	    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
	    [JsonIgnore]
	    public DateTime Updated { get; set; }

	    private bool _added;
	    [NotMapped]

	    public bool Added => Created != default(DateTime) || _added;

	    private DateTime _touched = DateTime.UtcNow;
	    [NotMapped]

	    public DateTime Touched => Added ? _touched : default(DateTime);

	    [JsonIgnore]
	    public readonly SemaphoreSlim Lock = new SemaphoreSlim(1);

	    public void MarkTouched()
	    {
	        _added = true;
	        _touched = DateTime.UtcNow;
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
