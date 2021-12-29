<script lang="ts">
	import TagList from '$lib/blog/TagList.svelte';
	import ShortDate from '$lib/ShortDate.svelte';
	import { onMount, tick } from 'svelte';
	import hl from '$lib/highlight';
	import type { BlogPostView } from '$lib/api';

	let content: HTMLElement;
	export let data: BlogPostView;

	async function renderCode() {
		if (!data.contentHtml) {
			return;
		}
		await tick();
		if (content != null) {
			hl.highlightAllUnder(content);
		}
	}

	$: data, renderCode();
</script>

<div class="readblock">
	<article bind:this={content} class="postbody content line-numbers">
		{@html data.contentHtml}
	</article>
	<hr />
	<div class="columns is-size-7 is-gapless">
		<div class="column">Posted <ShortDate date={data.createTime} /></div>
		<div class="column is-narrow">
			<TagList tags={data.tags} />
		</div>
	</div>
</div>

<style lang="scss">
	@import 'bulma/sass/grid/columns';

	hr {
		background-color: #2a392f;
	}

	:global {
		.postbody img {
			display: block;
			margin: 1em auto;
		}
	}
</style>
