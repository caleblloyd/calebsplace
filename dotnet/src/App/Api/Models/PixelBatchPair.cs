namespace App.Api.Models
{
    public class PixelBatchPair
    {
        public readonly PixelBatch MemoryBatch;

        public readonly PixelBatch DbBatch;

        public PixelBatchPair(PixelBatch memoryBatch, PixelBatch dbBatch)
        {
            MemoryBatch = memoryBatch;
            DbBatch = dbBatch;
        }
    }
}