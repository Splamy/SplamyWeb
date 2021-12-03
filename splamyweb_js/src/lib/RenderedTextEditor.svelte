<script lang="ts">
	import * as signalR from '@microsoft/signalr';
	import Icon from './Icon.svelte';
	import { BASE_URL, debounced } from './util';
	import { onMount } from 'svelte';
	import { mdiEye, mdiFlipHorizontal, mdiPencil } from '@mdi/js';

	const enum View {
		Edit,
		Both,
		Rendered
	}

	export let raw: string;

	let view: View = View.Both;
	let rendered: string = '';
	let connection: signalR.HubConnection | undefined = undefined;

	init();

	async function init() {
		try {
			connection = new signalR.HubConnectionBuilder().withUrl(`${BASE_URL}/markdown`).build();
			await connection.start();
		} catch (err) {
			console.error(err);
		}
	}

	$: renderRequest(raw);

	const renderRequest = debounced(
		(text: string) => {
			try {
				connection.invoke<string>('Render', text).then((r) => (rendered = r));
			} catch (e) {
				console.error('Failed to render text', e);
			}
		},
		100,
		{
			resetOnCall: false
		}
	);

	onMount(() => {
		return () => {
			connection?.stop();
			connection = undefined;
		};
	});
</script>

<div class="field has-addons">
	<p class="control">
		<button title="Source" class="button" on:click={() => (view = View.Edit)}>
			<Icon path={mdiPencil} />
		</button>
	</p>
	<p class="control">
		<button title="Split view" class="button" on:click={() => (view = View.Both)}>
			<Icon path={mdiFlipHorizontal} />
		</button>
	</p>
	<p class="control">
		<button title="Preview" class="button" on:click={() => (view = View.Rendered)}>
			<Icon path={mdiEye} />
		</button>
	</p>
</div>

<div class="editbox">
	{#if view === View.Edit || view === View.Both}
		<textarea bind:value={raw} />
	{/if}
	{#if view === View.Rendered || view === View.Both}
		<div class="renderSide content">
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
</style>
