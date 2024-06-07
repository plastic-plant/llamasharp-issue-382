using LLama.Common;
using LLama;

namespace Example2
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string modelPath = "all-minilm-sentence-transformer.gguf";

			var @params = new ModelParams(modelPath) { Embeddings = true };
			using var weights = LLamaWeights.LoadFromFile(@params);
			var embedder = new LLamaEmbedder(weights, @params);

			Console.WriteLine("This example generates embeddings from a text prompt. Please type your text:");
			var text = Console.ReadLine();

			float[] embeddings = embedder.GetEmbeddings(text).Result;
			Console.WriteLine($"Embeddings contain {embeddings.Length:N0} floating point values: {string.Join(", ", embeddings.Take(20))}, ...");
		}
	}
}
