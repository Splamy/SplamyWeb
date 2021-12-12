<script lang="ts">
	import type { BlogViewData } from '$lib/api';

	export let blogView: BlogViewData;
</script>

<article class="blogentry box">
	<h2 class="subtitle">
		{blogView.title}
	</h2>
	<div class="content">
		{@html blogView.summaryHtml}
	</div>
	<div>
		{#each blogView.tags as tag}
			<span>{tag}</span>
		{/each}
	</div>
</article>

<style lang="scss">
	.blogentry {
		padding: 1em;
	}

	.content {
		line-break: anywhere;
	}
</style>
