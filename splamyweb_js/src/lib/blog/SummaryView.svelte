<script lang="ts">
	import type { BlogPostShortView } from '$lib/api';
	import { CurrentUser } from '$lib/user';
	import PostFooter from './PostFooter.svelte';

	export let post: BlogPostShortView;
</script>

<article class="blogentry box">
	<div class="topline">
		<h2 class="subtitle" style="flex: 1;">
			<a href="/blog/post?i={post.postId}">
				{post.title}
			</a>
		</h2>
		{#if $CurrentUser}
			<div class="actions">
				<a href="/blog/editor?post={post.postId}">Edit</a>
			</div>
		{/if}
	</div>
	<div class="content">
		{@html post.summaryHtml}
	</div>
	<PostFooter {post} />
</article>

<style lang="scss">
	@import '../css/_prelude';
	@import 'bulma/sass/elements/title';
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
