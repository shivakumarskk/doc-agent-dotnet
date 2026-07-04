using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.HuggingFace.Tokenizer;
using DocAgent.Core.Interfaces;

namespace DocAgent.Infrastructure.Services;

/// <summary>
/// ONNX-based embedding service
/// </summary>
public class EmbeddingService : IEmbeddingService, IDisposable
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;
    private const int MAX_SEQUENCE_LENGTH = 512;

    public EmbeddingService(string modelPath, string tokenizerPath)
    {
        _session = new InferenceSession(modelPath);
        _tokenizer = Tokenizer.FromFile(tokenizerPath);
        LogModelMetadata();
    }

    private void LogModelMetadata()
    {
        Console.WriteLine("[EmbeddingService] Model Inputs:");
        foreach (var input in _session.InputNames)
        {
            var metadata = _session.InputMetadata[input];
            Console.WriteLine($"  - {input}: Shape=[{string.Join(", ", metadata.Dimensions)}], Type={metadata.ElementType}");
        }

        Console.WriteLine("[EmbeddingService] Model Outputs:");
        foreach (var output in _session.OutputNames)
        {
            var metadata = _session.OutputMetadata[output];
            Console.WriteLine($"  - {output}: Shape=[{string.Join(", ", metadata.Dimensions)}], Type={metadata.ElementType}");
        }
    }

    public float[] Embed(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty", nameof(text));

        var encodings = _tokenizer.Encode(text, addSpecialTokens: true);
        if (encodings == null || !encodings.Any())
            throw new InvalidOperationException("Tokenizer returned no encodings");

        var encoding = encodings.First();
        var inputIds = encoding.Ids.Select(x => (long)x).ToArray();

        if (inputIds.Length == 0)
            throw new InvalidOperationException("Tokenization produced empty sequence");

        Console.WriteLine($"[EmbeddingService] Input text length: {text.Length}");
        Console.WriteLine($"[EmbeddingService] Sequence length: {inputIds.Length}");
        Console.WriteLine($"[EmbeddingService] First 10 token IDs: {string.Join(", ", inputIds.Take(10))}");

        var attentionMaskSource = encoding.AttentionMask;
        long[] attentionMaskLong;
        float[] attentionMaskFloat;

        if (attentionMaskSource == null || attentionMaskSource.Count == 0)
        {
            Console.WriteLine($"[EmbeddingService] WARNING: Tokenizer returned empty attention mask. Generating default (all 1s).");
            attentionMaskLong = Enumerable.Repeat(1L, inputIds.Length).ToArray();
            attentionMaskFloat = Enumerable.Repeat(1f, inputIds.Length).ToArray();
        }
        else
        {
            Console.WriteLine($"[EmbeddingService] Attention mask length: {attentionMaskSource.Count}");
            attentionMaskLong = attentionMaskSource.Select(x => (long)x).ToArray();
            attentionMaskFloat = attentionMaskSource.Select(x => (float)x).ToArray();
        }

        if (attentionMaskLong.Length != inputIds.Length)
        {
            Console.WriteLine($"[EmbeddingService] WARNING: Attention mask length {attentionMaskLong.Length} doesn't match input IDs {inputIds.Length}. Adjusting...");

            if (attentionMaskLong.Length < inputIds.Length)
            {
                Array.Resize(ref attentionMaskLong, inputIds.Length);
                Array.Resize(ref attentionMaskFloat, inputIds.Length);
                for (int i = attentionMaskLong.Length - (inputIds.Length - attentionMaskLong.Length); i < inputIds.Length; i++)
                {
                    if (i < attentionMaskLong.Length)
                    {
                        attentionMaskLong[i] = 1;
                        attentionMaskFloat[i] = 1f;
                    }
                }
            }
            else
            {
                Array.Resize(ref attentionMaskLong, inputIds.Length);
                Array.Resize(ref attentionMaskFloat, inputIds.Length);
            }
        }

        if (inputIds.Length > MAX_SEQUENCE_LENGTH)
        {
            Console.WriteLine($"[EmbeddingService] Trimming sequence from {inputIds.Length} to {MAX_SEQUENCE_LENGTH}");
            Array.Resize(ref inputIds, MAX_SEQUENCE_LENGTH);
            Array.Resize(ref attentionMaskLong, MAX_SEQUENCE_LENGTH);
            Array.Resize(ref attentionMaskFloat, MAX_SEQUENCE_LENGTH);
        }

        var tokenTypeIds = new long[inputIds.Length];

        var inputTensor = new DenseTensor<long>(inputIds, new int[] { 1, inputIds.Length });
        var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, new int[] { 1, tokenTypeIds.Length });
        var attentionMaskTensor = new DenseTensor<long>(attentionMaskLong, new int[] { 1, attentionMaskLong.Length });

        Console.WriteLine($"[EmbeddingService] Input tensor shape: [1, {inputIds.Length}]");
        Console.WriteLine($"[EmbeddingService] Attention mask shape: [1, {attentionMaskLong.Length}]");
        Console.WriteLine($"[EmbeddingService] Token type IDs shape: [1, {tokenTypeIds.Length}]");

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
        };

        try
        {
            using var results = _session.Run(inputs);

            var outputTensor = results.First(r => r.Name == "last_hidden_state").AsTensor<float>();
            var outputArray = outputTensor.ToArray();

            Console.WriteLine($"[EmbeddingService] Output shape: {string.Join(", ", outputTensor.Dimensions.ToArray())}");

            int sequenceLength = inputIds.Length;
            int hiddenSize = outputArray.Length / sequenceLength;

            if (hiddenSize <= 0)
                throw new InvalidOperationException($"Invalid hidden size calculation: {outputArray.Length} / {sequenceLength}");

            var embedding = new float[hiddenSize];
            float totalMask = 0f;

            for (int i = 0; i < sequenceLength; i++)
            {
                float mask = attentionMaskFloat[i];
                totalMask += mask;

                for (int j = 0; j < hiddenSize; j++)
                {
                    embedding[j] += outputArray[i * hiddenSize + j] * mask;
                }
            }

            if (totalMask > 0)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    embedding[j] /= totalMask;
                }
            }

            return embedding;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmbeddingService] ERROR: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
