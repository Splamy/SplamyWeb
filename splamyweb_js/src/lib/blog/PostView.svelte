<script lang="ts">
	import { tick } from 'svelte';
	import hl from '$lib/highlight';
	import type { BlogPostView } from '$lib/api';
	import PostFooter from './PostFooter.svelte';

	let content: HTMLElement;
	export let post: BlogPostView;

	async function renderCode() {
		if (!post.contentHtml) {
			return;
		}
		await tick();
		if (content != null) {
			hl.highlightAllUnder(content);
		}
	}

	$: post, renderCode();
</script>

<div class="readblock">
	<article bind:this={content} class="postbody content line-numbers">
		{@html post.contentHtml}
	</article>
	<hr />
	<PostFooter {post} />
</div>

<style lang="scss">
	hr {
		background-color: #2a392f;
	}

	:global(.postbody img) {
		display: block;
		margin: 1em auto;
	}
</style>
