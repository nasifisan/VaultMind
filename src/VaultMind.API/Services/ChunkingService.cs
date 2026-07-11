using System.Text.RegularExpressions;
using VaultMind.API.Interfaces;
using VaultMind.API.Models;

namespace VaultMind.API.Services;

public class ChunkingService : IChunkingService
{
    private readonly ILogger<ChunkingService> _logger;
    private readonly int _maxTokensPerChunk;
    private readonly int _overlapTokens;

    public ChunkingService(IConfiguration configuration, ILogger<ChunkingService> logger)
    {
        _logger = logger;

        // Load configurations with sensible defaults (500 tokens max, 50 tokens overlap)
        _maxTokensPerChunk = configuration.GetValue<int>("Chunking:MaxTokensPerChunk", 500);
        _overlapTokens = configuration.GetValue<int>("Chunking:OverlapTokens", 50);

        if (_maxTokensPerChunk <= 0) _maxTokensPerChunk = 500;
        if (_overlapTokens < 0) _overlapTokens = 50;
        if (_overlapTokens >= _maxTokensPerChunk) _overlapTokens = _maxTokensPerChunk / 10;

        _logger.LogInformation("ChunkingService initialized with MaxTokensPerChunk: {MaxTokens}, OverlapTokens: {Overlap}", _maxTokensPerChunk, _overlapTokens);
    }

    public List<DocumentChunk> ChunkText(string text, Guid documentId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<DocumentChunk>();
        }

        var chunks = new List<DocumentChunk>();

        // Regex to split text on sentence boundaries: dot, question mark, or exclamation mark followed by whitespace
        var sentences = Regex.Split(text, @"(?<=[.?!])\s+");

        var currentChunkSentences = new List<string>();
        int currentTokenCount = 0;
        int chunkIndex = 0;

        for (int i = 0; i < sentences.Length; i++)
        {
            var sentence = sentences[i].Trim();
            if (string.IsNullOrEmpty(sentence)) continue;

            int sentenceTokens = EstimateTokens(sentence);

            // If a single sentence exceeds the chunk budget, we must break it down by words
            if (sentenceTokens > _maxTokensPerChunk)
            {
                // Flush existing chunk sentences first
                if (currentChunkSentences.Count > 0)
                {
                    chunks.Add(CreateChunk(currentChunkSentences, documentId, fileName, chunkIndex++));
                    currentChunkSentences.Clear();
                    currentTokenCount = 0;
                }

                // Chunk the huge sentence by words
                var words = sentence.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                var currentWordList = new List<string>();
                int wordTokenCount = 0;

                for (int w = 0; w < words.Length; w++)
                {
                    currentWordList.Add(words[w]);
                    int wordTokens = (int)Math.Ceiling(words[w].Length / 4.0); // simple character-based token approximation per word
                    wordTokenCount += wordTokens;

                    if (wordTokenCount >= _maxTokensPerChunk || w == words.Length - 1)
                    {
                        chunks.Add(new DocumentChunk
                        {
                            DocumentId = documentId,
                            FileName = fileName,
                            ChunkIndex = chunkIndex++,
                            Content = string.Join(" ", currentWordList)
                        });

                        // Set up overlap by keeping last N words that fit within overlap limit
                        var overlapWords = new List<string>();
                        int overlapTokenSum = 0;
                        for (int k = currentWordList.Count - 1; k >= 0; k--)
                        {
                            int wTokens = (int)Math.Ceiling(currentWordList[k].Length / 4.0);
                            if (overlapTokenSum + wTokens <= _overlapTokens)
                            {
                                overlapWords.Insert(0, currentWordList[k]);
                                overlapTokenSum += wTokens;
                            }
                            else
                            {
                                break;
                            }
                        }

                        currentWordList = overlapWords;
                        wordTokenCount = overlapTokenSum;
                    }
                }
                continue;
            }

            // If adding the current sentence would overflow the token budget, flush current chunk
            if (currentTokenCount + sentenceTokens > _maxTokensPerChunk)
            {
                chunks.Add(CreateChunk(currentChunkSentences, documentId, fileName, chunkIndex++));

                // Implement overlap: pull the last N sentences that fit within the overlap token budget
                var overlapSentences = new List<string>();
                int overlapTokenSum = 0;
                for (int j = currentChunkSentences.Count - 1; j >= 0; j--)
                {
                    var s = currentChunkSentences[j];
                    int sTokens = EstimateTokens(s);
                    if (overlapTokenSum + sTokens <= _overlapTokens)
                    {
                        overlapSentences.Insert(0, s);
                        overlapTokenSum += sTokens;
                    }
                    else
                    {
                        break;
                    }
                }

                currentChunkSentences = overlapSentences;
                currentTokenCount = overlapTokenSum;
            }

            currentChunkSentences.Add(sentence);
            currentTokenCount += sentenceTokens;
        }

        // Flush any remaining text as the last chunk
        if (currentChunkSentences.Count > 0)
        {
            chunks.Add(CreateChunk(currentChunkSentences, documentId, fileName, chunkIndex++));
        }

        _logger.LogInformation("Completed chunking for document {FileName} ({DocumentId}). Created {Count} chunks.", fileName, documentId, chunks.Count);
        return chunks;
    }

    private DocumentChunk CreateChunk(List<string> sentences, Guid documentId, string fileName, int index)
    {
        return new DocumentChunk
        {
            DocumentId = documentId,
            FileName = fileName,
            ChunkIndex = index,
            Content = string.Join(" ", sentences)
        };
    }

    /// <summary>
    /// Estimates token count using standard 4 characters per token heuristic (or 1.3 tokens per word)
    /// </summary>
    private int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        // A hybrid approach: count words and apply a factor of 1.3 tokens per word,
        // while also checking character count.
        var words = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        int wordBasedCount = (int)Math.Ceiling(words.Length * 1.3);
        int charBasedCount = (int)Math.Ceiling(text.Length / 4.0);

        // Take the average of both heuristics for a more robust token approximation
        return (wordBasedCount + charBasedCount) / 2;
    }
}
