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
    }

    private void LogModelMetadata()
    {
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

        var attentionMaskSource = encoding.AttentionMask;
        long[] attentionMaskLong;
        float[] attentionMaskFloat;

        if (attentionMaskSource == null || attentionMaskSource.Count == 0)
        {
            attentionMaskLong = Enumerable.Repeat(1L, inputIds.Length).ToArray();
            attentionMaskFloat = Enumerable.Repeat(1f, inputIds.Length).ToArray();
        }
        else
        {
            attentionMaskLong = attentionMaskSource.Select(x => (long)x).ToArray();
            attentionMaskFloat = attentionMaskSource.Select(x => (float)x).ToArray();
        }

        if (attentionMaskLong.Length != inputIds.Length)
        {
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
            Array.Resize(ref inputIds, MAX_SEQUENCE_LENGTH);
            Array.Resize(ref attentionMaskLong, MAX_SEQUENCE_LENGTH);
            Array.Resize(ref attentionMaskFloat, MAX_SEQUENCE_LENGTH);
        }

        var tokenTypeIds = new long[inputIds.Length];

        var inputTensor = new DenseTensor<long>(inputIds, new int[] { 1, inputIds.Length });
        var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, new int[] { 1, tokenTypeIds.Length });
        var attentionMaskTensor = new DenseTensor<long>(attentionMaskLong, new int[] { 1, attentionMaskLong.Length });

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
        catch (Exception)
        {
            throw;
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
