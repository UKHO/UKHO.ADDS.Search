using System.Threading.Channels;

namespace UKHO.Search.Pipelines.Channels
{
	public static class BoundedChannelFactory
	{
		public static Channel<T> Create<T>(int capacity, bool singleReader = false, bool singleWriter = false)
		{
			var options = new BoundedChannelOptions(capacity)
			{
				SingleReader = singleReader,
				SingleWriter = singleWriter,
				FullMode = BoundedChannelFullMode.Wait,
			};

			return Channel.CreateBounded<T>(options);
		}
	}
}
