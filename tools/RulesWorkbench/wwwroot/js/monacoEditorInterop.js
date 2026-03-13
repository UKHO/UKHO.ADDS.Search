let monacoLoaderPromise;

function ensureMonaco() {
	if (monacoLoaderPromise) {
		return monacoLoaderPromise;
	}

	monacoLoaderPromise = new Promise((resolve, reject) => {
		if (window.monaco && window.monaco.editor) {
			resolve(window.monaco);
			return;
		}

		const requireConfigured = window.require && window.require.config;
		if (!requireConfigured) {
			reject(new Error('Monaco loader (require.js) not found.')); 
			return;
		}

		window.require.config({ paths: { vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/vs' } });
		window.require(['vs/editor/editor.main'], () => {
			resolve(window.monaco);
		}, reject);
	});

	return monacoLoaderPromise;
}

export async function createJsonEditor(hostElement, value, isReadOnly, enableFolding, dotNetRef) {
	const monaco = await ensureMonaco();

	const editor = monaco.editor.create(hostElement, {
		value: value ?? '',
		language: 'json',
		readOnly: isReadOnly === true,
		automaticLayout: true,
		minimap: { enabled: false },
		folding: enableFolding === true,
		lineNumbers: 'on',
		scrollBeyondLastLine: false,
	});

	const model = editor.getModel();
	let suppress = false;

	if (model) {
		model.onDidChangeContent(() => {
			if (suppress) {
				return;
			}

			const current = editor.getValue();
			dotNetRef.invokeMethodAsync('OnEditorValueChanged', current);
		});
	}

	return {
		editor,
		setSuppress: (v) => { suppress = v; },
	};
}

export function setValue(editorHandle, value) {
	if (!editorHandle || !editorHandle.editor) {
		return;
	}

	editorHandle.setSuppress(true);
	try {
		editorHandle.editor.setValue(value ?? '');
	}
	finally {
		editorHandle.setSuppress(false);
	}
}

export function dispose(editorHandle) {
	if (!editorHandle || !editorHandle.editor) {
		return;
	}

	try {
		editorHandle.editor.dispose();
	}
	catch {
		// ignore
	}
}
