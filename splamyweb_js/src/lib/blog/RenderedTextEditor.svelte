<script lang="ts">
	import * as signalR from '@microsoft/signalr';
	import { autosize, BASE_URL, debounced } from '$lib/util';
	import { onMount } from 'svelte';
	import { prerendering } from '$app/env';
	import { View } from './editor';
	import PostView from './PostView.svelte';
	import { BlogPostUpdate, BlogPostView, EMPTY_POST } from '$lib/api';

	export let raw: string = '';
	export let postEdit: BlogPostUpdate = {};
	export let view: View = View.Edit;

	let post: BlogPostView = EMPTY_POST();
	let connection: signalR.HubConnection | undefined = undefined;

	if (!prerendering) {
		init();
	}

	async function init() {
		try {
			connection = new signalR.HubConnectionBuilder().withUrl(`${BASE_URL}/markdown`).build();
			await connection.start();
		} catch (err) {
			console.warn('Failed to establish connection: ', err?.message);
		}
	}

	async function tryRender() {
		try {
			if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
				return;
			}
			let rendered = await connection.invoke<string>('Render', raw);
			post = Object.assign(post, postEdit);
			post.contentHtml = rendered;
		} catch (err) {
			console.warn('Failed to render text: ', err?.message);
		}
	}

	const tryRenderDebounced = debounced(tryRender, 100, {
		resetOnCall: false
	});

	$: raw, tryRenderDebounced();

	onMount(() => {
		return () => {
			connection?.stop();
			connection = undefined;
		};
	});
</script>

<div class="editbox">
	{#if view === View.Edit || view === View.Both}
		<textarea class="input" use:autosize bind:value={raw} />
	{/if}
	{#if view === View.Rendered || view === View.Both}
		<div>
			<PostView {post} />
		</div>
	{/if}
</div>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/form/shared';
	@import 'bulma/sass/form/input-textarea';
	@import 'bulma/sass/elements/box';

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
