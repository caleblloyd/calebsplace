namespace App.Api.Models
{
    public class PixelUpdate
    {
        public readonly Pixel From;

        public readonly Pixel To;

        public PixelUpdate(Pixel from, Pixel to)
        {
            From = from;
            To = to;
        }
    }
}