window.rulesWorkbench = window.rulesWorkbench || {};
window.rulesWorkbench.clipboard = {
	copyText: async function (text) {
		if (!navigator.clipboard) {
			throw new Error('Clipboard API not available');
		}
		await navigator.clipboard.writeText(text);
	}
};
