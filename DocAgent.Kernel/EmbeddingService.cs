using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.HuggingFace.Tokenizer;

public class EmbeddingService : IDisposable
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;
    private const int MAX_SEQUENCE_LENGTH = 512;

    public EmbeddingService(string modelPath, string tokenizerPath)
    {
        _session = new InferenceSession(modelPath);
        _tokenizer = Tokenizer.FromFile(tokenizerPath); // tokenizer.json

        // Log model metadata for debugging
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
        // Validate input
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty", nameof(text));

        // Tokenize input
        var encodings = _tokenizer.Encode(text, addSpecialTokens: true);
        if (encodings == null || !encodings.Any())
            throw new InvalidOperationException("Tokenizer returned no encodings");

        var encoding = encodings.First();
        var inputIds = encoding.Ids.Select(x => (long)x).ToArray();

        // Validate tokenization result
        if (inputIds.Length == 0)
            throw new InvalidOperationException("Tokenization produced empty sequence");

        Console.WriteLine($"[EmbeddingService] Input text length: {text.Length}");
        Console.WriteLine($"[EmbeddingService] Sequence length: {inputIds.Length}");
        Console.WriteLine($"[EmbeddingService] First 10 token IDs: {string.Join(", ", inputIds.Take(10))}");

        // Get attention mask - if empty, generate one (all 1s by default)
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

        // Validate attention mask matches input length
        if (attentionMaskLong.Length != inputIds.Length)
        {
            Console.WriteLine($"[EmbeddingService] WARNING: Attention mask length {attentionMaskLong.Length} doesn't match input IDs {inputIds.Length}. Adjusting...");

            // If attention mask is shorter, pad it with 1s
            if (attentionMaskLong.Length < inputIds.Length)
            {
                Array.Resize(ref attentionMaskLong, inputIds.Length);
                Array.Resize(ref attentionMaskFloat, inputIds.Length);
                // Fill the padded portion with 1s (or pad tokens should be 1s for this logic)
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
                // If attention mask is longer, trim it
                Array.Resize(ref attentionMaskLong, inputIds.Length);
                Array.Resize(ref attentionMaskFloat, inputIds.Length);
            }
        }

        // Trim sequences that exceed max length
        if (inputIds.Length > MAX_SEQUENCE_LENGTH)
        {
            Console.WriteLine($"[EmbeddingService] Trimming sequence from {inputIds.Length} to {MAX_SEQUENCE_LENGTH}");
            Array.Resize(ref inputIds, MAX_SEQUENCE_LENGTH);
            Array.Resize(ref attentionMaskLong, MAX_SEQUENCE_LENGTH);
            Array.Resize(ref attentionMaskFloat, MAX_SEQUENCE_LENGTH);
        }

        // Create token_type_ids (all zeros for single sentence)
        var tokenTypeIds = new long[inputIds.Length];

        // Build tensors
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

        // Run inference
        try
        {
            using var results = _session.Run(inputs);

            // Get the output tensor (last_hidden_state)
            var outputTensor = results.First(r => r.Name == "last_hidden_state").AsTensor<float>();
            var outputArray = outputTensor.ToArray();

            Console.WriteLine($"[EmbeddingService] Output shape: {string.Join(", ", outputTensor.Dimensions.ToArray())}");

            // Mean pooling with attention mask
            // Output shape: [1, sequence_length, hidden_size]
            int sequenceLength = inputIds.Length;
            int hiddenSize = outputArray.Length / sequenceLength;

            if (hiddenSize <= 0)
                throw new InvalidOperationException($"Invalid hidden size calculation: {outputArray.Length} / {sequenceLength}");

            var pooled = new float[hiddenSize];

            // Apply mean pooling with attention mask (using float version for calculations)
            for (int i = 0; i < sequenceLength; i++)
            {
                float maskValue = attentionMaskFloat[i];
                for (int j = 0; j < hiddenSize; j++)
                {
                    pooled[j] += outputArray[i * hiddenSize + j] * maskValue;
                }
            }

            // Normalize by sum of attention mask (to handle padding)
            float maskSum = attentionMaskFloat.Sum();
            if (maskSum <= 0)
                throw new InvalidOperationException("Attention mask sum is zero or negative");

            for (int j = 0; j < hiddenSize; j++)
            {
                pooled[j] /= maskSum;
            }

            // Normalize embeddings (L2 normalization)
            float norm = (float)Math.Sqrt(pooled.Sum(x => x * x));
            if (norm > 0)
            {
                for (int i = 0; i < pooled.Length; i++)
                {
                    pooled[i] /= norm;
                }
            }

            Console.WriteLine($"[EmbeddingService] Embedding generated successfully (size: {pooled.Length})");
            return pooled;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmbeddingService] Error during inference: {ex.Message}");
            throw;
        }
    }

    public void Dispose() => _session.Dispose();
}
