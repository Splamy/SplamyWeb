<script context="module" lang="ts">
	import { BASE_URL } from '$lib/util';
	import type { BlogPostUpdate } from '$lib/api';
	import type { Load } from '@sveltejs/kit';
	import { prerendering } from '$app/env';

	export const load: Load = async ({ fetch, page }) => {
		if (prerendering) return {};

		const post = page.query.get('post');
		if (post) {
			const res = await fetch(`${BASE_URL}/api/content/post/${post}/raw`, {
				credentials: 'include'
			});
			const json: BlogPostUpdate = await res.json();

			if (res.ok) {
				return {
					props: {
						data: json
					}
				};
			}

			return { status: res.status };
		}
		return {};
	};
</script>

<script lang="ts">
	import swal from 'sweetalert';
	import RenderedTextEditor from '$lib/blog/RenderedTextEditor.svelte';
	import TextEditorMode from '$lib/blog/TextEditorMode.svelte';
	import type { View } from '$lib/blog/editor';
	import { goto } from '$app/navigation';
	import TagEditor from '$lib/blog/TagEditor.svelte';

	export let data: BlogPostUpdate = {
		visible: true,
		tags: []
	};
	$: data.tags ??= [];

	// ?post=<number>
	let view: View;
	let updating = false;

	async function savePost() {
		if (updating) return;
		updating = true;
		try {
			const res = await fetch(`${BASE_URL}/api/content/post`, {
				method: 'PUT',
				credentials: 'include',
				headers: {
					'Content-Type': 'application/json'
				},
				body: JSON.stringify(data)
			});
			if (!res.ok) throw new Error(`${res.status}: ${await res.text()}`);

			const update = await res.json();
			data = Object.assign(data, update);
		} catch (err) {
			await swal(err);
		} finally {
			updating = false;
		}
	}

	async function deletePost() {
		if (updating) return;
		updating = true;
		try {
			if (!(await confirmDelete())) return;
			if (data.postId) {
				const res = await fetch(`${BASE_URL}/api/content/post/${data.postId}`, {
					method: 'DELETE',
					credentials: 'include'
				});
				if (!res.ok) throw new Error(`${res.status}: ${await res.text()}`);
			}
			await goto('/blog');
		} catch (err) {
			await swal(err);
		} finally {
			updating = false;
		}
	}

	async function confirmDelete(): Promise<boolean> {
		const hasId = data.postId !== undefined;
		if (!hasId && !data.contentRaw) return true;
		const askText = hasId
			? 'Are you sure you want to delete this post?'
			: 'Are you sure you want to leave this page?';
		const answer = await swal(askText, {
			dangerMode: true,
			buttons: ['Cancel', hasId ? 'Delete' : 'Leave']
		});
		return answer as boolean;
	}
</script>

<div class="columns">
	<form class="column is-narrow">
		<div class="field">
			<label for="post_visible">Post visible</label>
			<input type="checkbox" id="post_visible" bind:checked={data.visible} />
		</div>
		<div class="field">
			<TagEditor bind:tags={data.tags} />
		</div>
		<hr />
		<TextEditorMode bind:view />
		<div class="field">
			<button
				class="button"
				on:click|preventDefault|stopPropagation={savePost}
				disabled={updating}>Save</button
			>
		</div>
		<div class="field">
			<button
				class="button is-danger is-outlined"
				on:click|preventDefault|stopPropagation={deletePost}
				disabled={updating}
			>
				{#if data.postId}
					Delete
				{:else}
					Cancel
				{/if}
			</button>
		</div>
	</form>
	<div class="column">
		<div class="field">
			<RenderedTextEditor bind:raw={data.contentRaw} bind:view />
		</div>
	</div>
</div>

<style lang="scss">
	@import 'bulma/sass/grid/columns';
</style>
