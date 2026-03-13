namespace RulesWorkbench.Services
{
	public interface IClipboardService
	{
		ValueTask CopyTextAsync(string text);
	}
}
