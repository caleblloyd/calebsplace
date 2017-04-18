using Microsoft.EntityFrameworkCore;

namespace App.Api.Models{

    public class Pixel
	{
		public int X { get; set; }
		public int Y { get; set; }
		public byte[] Color { get; set; } = new byte[3];
	}

	public static class PixelMeta
	{
		public static void OnModelCreating(ModelBuilder modelBuilder){

            modelBuilder.Entity<Pixel>(entity => {
                entity.HasKey(m => new {m.X, m.Y});
            });
			
        }
	}
    
}