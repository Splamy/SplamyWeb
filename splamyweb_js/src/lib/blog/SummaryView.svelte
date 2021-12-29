<script lang="ts">
	import type { BlogPostShortView } from '$lib/api';
	import ShortDate from '$lib/ShortDate.svelte';
	import { CurrentUser } from '$lib/user';
	import TagList from './TagList.svelte';

	export let blogView: BlogPostShortView;

	function colorHash(str: string): string {
		let hash = 0;
		for (let i = 0; i < str.length; i++) {
			hash = str.charCodeAt(i) + ((hash << 5) - hash);
		}
		return `hsl(${hash % 360}, 25%, 25%)`;
	}
</script>

<article class="blogentry box">
	<div class="topline">
		<h2 class="subtitle" style="flex: 1;">
			<a href="/blog/post?i={blogView.postId}">
				{blogView.title}
			</a>
		</h2>
		{#if $CurrentUser}
			<div class="actions">
				<a href="/blog/editor?post={blogView.postId}">Edit</a>
			</div>
		{/if}
	</div>
	<div class="content">
		{@html blogView.summaryHtml}
	</div>
	<div class="columns is-size-7 is-gapless">
		<div class="column">
			Posted <ShortDate date={blogView.createTime} />
			{#if blogView.visible === false}
				<span class="tag is-warning">Hidden</span>
			{/if}
		</div>
		<div class="column is-narrow">
			<TagList tags={blogView.tags} />
		</div>
	</div>
</article>

<style lang="scss">
	@import '../css/_prelude';
	@import 'bulma/sass/elements/tag';
	@import 'bulma/sass/elements/title';
	@import 'bulma/sass/grid/columns';
	@import 'bulma/sass/elements/box';

	.topline {
		display: flex;
		flex-direction: row;
	}

	.subtitle {
		text-decoration: underline;
	}

	.blogentry {
		padding: 1em;

		&:not(:last-child) {
			//border-bottom: 2px solid #2a392f;
		}
	}

	.tags {
		display: flex;
		justify-content: end;
	}

	.tag {
		color: rgb(200, 200, 200);

		// &:nth-child(-n + 4) {
		// 	display: block;
		// }
	}

	hr {
		background-color: #2a392f;
	}

	.actions {
		padding: 0 0.5em;
	}
</style>
