<script lang="ts">
	import type * as monaco from 'monaco-editor';
	import { onMount, onDestroy } from 'svelte';

	//import * as Monaco from 'monaco-editor';
	import editorWorker from 'monaco-editor/editor/editor.worker?worker';
	import jsonWorker from 'monaco-editor/language/json/json.worker?worker';
	//import cssWorker from 'monaco-editor/esm/vs/language/css/css.worker?worker';
	//import htmlWorker from 'monaco-editor/esm/vs/language/html/html.worker?worker';
	//import tsWorker from 'monaco-editor/esm/vs/language/typescript/ts.worker?worker';

	export let content: string;
	export let language: string = 'json';
	export let readOnly: boolean = false;

	let _text = content;

	let divEl;
	let editor;
	let Monaco;

	function relayout() {
		editor.layout({ width: 0, height: 0 });
		window.requestAnimationFrame(() => {
			const rect = divEl.parentElement.getBoundingClientRect();
			editor.layout({ width: rect.width, height: rect.height });
		});
	}

	$: if (editor && content !== _text) {
		editor.setValue(content);
	}

	onMount(async () => {
		self.MonacoEnvironment = {
			getWorker: function (_moduleId, label) {
				if (label === 'json') {
					return new jsonWorker();
				}
				// if (label === 'css' || label === 'scss' || label === 'less') {
				// 	return new cssWorker();
				// }
				// if (label === 'html' || label === 'handlebars' || label === 'razor') {
				// 	return new htmlWorker();
				// }
				// if (label === 'typescript' || label === 'javascript') {
				// 	return new tsWorker();
				// }
				return new editorWorker();
			}
		};

		Monaco = await import('monaco-editor');
		editor = Monaco.editor.create(divEl, <monaco.editor.IEditorOptions>{
			value: content,
			language: language,
			theme: 'vs-dark',
			readOnly: readOnly,
			minimap: { enabled: false }
		});
		editor.onDidChangeModelContent(() => {
			if (readOnly) return;
			_text = editor.getValue();
			content = _text;
			//subscriptions.forEach((sub) => sub(text));
		});

		// content = {
		// 	subscribe(func) {
		// 		subscriptions.push(func);
		// 		return () => {
		// 			subscriptions = subscriptions.filter((sub) => sub != func);
		// 		};
		// 	},
		// 	update(updater) {
		// 		const text = editor.getValue();
		// 		const value = updater(text);
		// 		editor.setValue(value);
		// 	},
		// 	set(val) {
		// 		editor.setValue(val);
		// 	}
		// };
		relayout();
	});

	onDestroy(() => {
		if (editor) {
			editor.dispose();
		}
	});
</script>

<div class="flex-grow">
	<div bind:this={divEl} class="h-full w-full" />
</div>

<svelte:window on:resize={relayout} />

<style>
	.flex-grow {
		flex-grow: 1;
	}
</style>
