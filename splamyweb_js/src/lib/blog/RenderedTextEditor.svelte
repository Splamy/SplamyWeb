<script lang="ts">
	import * as signalR from '@microsoft/signalr';
	import { BASE_URL, debounced } from '../util';
	import { onMount } from 'svelte';
	import { prerendering } from '$app/env';
	import { View } from './editor';

	export let raw: string = "";
	export let view: View = View.Edit;

	let textArea: HTMLTextAreaElement;
	let rendered: string = '';
	let connection: signalR.HubConnection | undefined = undefined;

	if (!prerendering) {
		init();
	}

	async function init() {
		try {
			connection = new signalR.HubConnectionBuilder().withUrl(`${BASE_URL}/markdown`).build();
			await connection.start();
		} catch (err) {
			console.error(err);
		}
	}

	const renderRequest = debounced(
		async (text: string) => {
			try {
				if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
					return;
				}
				rendered = await connection.invoke<string>('Render', text);
			} catch (e) {
				console.error('Failed to render text', e);
			}
		},
		100,
		{
			resetOnCall: false
		}
	);

	$: renderRequest(raw);

	function adaptHeight() {
		if (!textArea) return;
		const oldH = textArea.clientHeight;
		textArea.style.height = 'auto';
		textArea.style.height = Math.max(oldH, textArea.scrollHeight) + 'px';
	}

	$: if (textArea) adaptHeight();

	onMount(() => {
		return () => {
			connection?.stop();
			connection = undefined;
		};
	});
</script>

<div class="editbox">
	{#if view === View.Edit || view === View.Both}
		<textarea on:input={adaptHeight} bind:this={textArea} bind:value={raw} />
	{/if}
	{#if view === View.Rendered || view === View.Both}
		<div class="renderSide readblock content">
			{@html rendered}
		</div>
	{/if}
</div>

<style lang="scss">
	.renderSide {
		overflow: hidden;
		padding: 0.5em;
	}

	.editbox {
		display: flex;

		> :global(*) {
			flex-grow: 1;
			flex-basis: 0;
		}
	}

	textarea {
		box-sizing: content-box;
	}
</style>
