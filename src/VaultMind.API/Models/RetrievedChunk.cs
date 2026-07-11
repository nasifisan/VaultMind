namespace VaultMind.API.Models;

public class RetrievedChunk
{
    public string Content { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public double Score { get; set; }
}
