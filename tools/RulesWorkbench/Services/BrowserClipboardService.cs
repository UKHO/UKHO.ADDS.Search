using Microsoft.JSInterop;

namespace RulesWorkbench.Services
{
	public sealed class BrowserClipboardService : IClipboardService
	{
		private readonly IJSRuntime _jsRuntime;

		public BrowserClipboardService(IJSRuntime jsRuntime)
		{
			_jsRuntime = jsRuntime;
		}

		public ValueTask CopyTextAsync(string text)
		{
			return _jsRuntime.InvokeVoidAsync("rulesWorkbench.clipboard.copyText", text);
		}
	}
}
