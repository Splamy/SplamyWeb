<script context="module" lang="ts">
	import { BASE_URL } from '$lib/util';
	import type { BlogViewData } from '$lib/api';
	import type { Load } from '@sveltejs/kit';
	import { prerendering } from '$app/env';

	export const load: Load = async ({ fetch, page }) => {
		if (prerendering) return {};

		const post = page.params.id;
		if (post) {
			const res = await fetch(`${BASE_URL}/api/content/post/${post}`);
			const json: BlogViewData = await res.json();

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
	import TagList from '$lib/blog/TagList.svelte';
	import ShortDate from '$lib/ShortDate.svelte';
	import moment from 'moment';

	export let data: BlogViewData = {
		title: 'Not Found',
		contentHtml: '',
		createTime: '',
		tags: []
	};
	$: recentPosts = data.recentPosts ?? [];
</script>

<div class="columns">
	<div class="column is-half is-offset-one-quarter">
		<article class="content readblock">
			{@html data.contentHtml}
		</article>
		<hr />
		<div class="columns is-size-7 is-gapless">
			<div class="column">Posted <ShortDate date={moment(data.createTime)} /></div>
			<div class="column is-narrow">
				<TagList tags={data.tags} />
			</div>
		</div>
	</div>
	<div class="column is-narrow">
		<div class="box">
			<h3 class="title is-4">Recent Posts</h3>
			<ul>
				{#each recentPosts as post}
					<li>
						<a href={`/blog/post/${post.postId}`}>
							{post.title}
						</a>
					</li>
				{/each}
			</ul>
		</div>
	</div>
</div>

<style lang="scss">
	@import '../../../lib/css/_prelude';
	@import 'bulma/sass/elements/title';
	@import 'bulma/sass/grid/columns';

	.content {
		line-break: anywhere;
	}

	hr {
		background-color: #2a392f;
	}
</style>
