namespace CateringSaaS.Modules.Knowledge.Services;

public sealed record TextChunk(int Index, string Content, int TokenEstimate);

public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text);
}
