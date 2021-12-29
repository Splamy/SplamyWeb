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
						data: json,
						postId: post
					}
				};
			}

			return { status: res.status };
		}
		return {};
	};
</script>

<script lang="ts">
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import RenderedTextEditor from '$lib/blog/RenderedTextEditor.svelte';
	import swal from 'sweetalert';
	import TagEditor from '$lib/blog/TagEditor.svelte';
	import TextEditorMode from '$lib/blog/TextEditorMode.svelte';
	import type { View } from '$lib/blog/editor';

	export let postId: string | undefined = undefined;
	export let data: BlogPostUpdate = {
		visible: true,
		tags: []
	};

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

	async function fetchPost(post: string) {
		try {
			const res = await fetch(`${BASE_URL}/api/content/post/${post}/raw`, {
				credentials: 'include'
			});
			const json: BlogPostUpdate = await res.json();

			if (res.ok) {
				data = json;
			}
		} catch (err) {}
	}

	onMount(() => {
		let queryParams = new URLSearchParams(window.location.search);
		const queryPostId = queryParams.get('post');
		if (postId !== queryPostId && queryPostId) {
			fetchPost(queryPostId);
		}
	});
</script>

<div class="columns">
	<form class="column is-narrow">
		<div class="sticky">
			<div class="field">
				<label for="post_visible">Post visible</label>
				<input
					class="input"
					type="checkbox"
					id="post_visible"
					bind:checked={data.visible}
				/>
			</div>
			<div class="field">
				<TagEditor bind:tags={data.tags} />
			</div>
			<hr />
			<TextEditorMode bind:view />
			<div class="field">
				<div class="buttons">
					<button
						class="button"
						on:click|preventDefault|stopPropagation={savePost}
						disabled={updating}>Save</button
					>
					{#if data.postId}
						<a class="button" href={`/blog/post?i=${data.postId}`}>View</a>
					{/if}
				</div>
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
		</div>
	</form>
	<div class="column">
		<div class="field">
			<RenderedTextEditor bind:raw={data.contentRaw} post={data} bind:view />
		</div>
	</div>
</div>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/grid/columns';
	@import 'bulma/sass/elements/button';
	@import 'bulma/sass/form/shared';
	@import 'bulma/sass/form/tools';
</style>
