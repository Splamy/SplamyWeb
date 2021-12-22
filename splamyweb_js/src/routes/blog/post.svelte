<script context="module" lang="ts">
	import { BASE_URL } from '$lib/util';
	import type { BlogItemQuery } from '$lib/api';
	import type { Load } from '@sveltejs/kit';
	import { prerendering } from '$app/env';

	export const load: Load = async ({ fetch, page }) => {
		if (prerendering) return {};

		const post = page.query.get('i');
		if (post) {
			const res = await fetch(`${BASE_URL}/api/content/post/${post}`, {
				credentials: 'include'
			});
			const json: BlogItemQuery = await res.json();

			if (res.ok) {
				return {
					props: {
						query: json,
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
	import TagList from '$lib/blog/TagList.svelte';
	import ShortDate from '$lib/ShortDate.svelte';
	import moment from 'moment';
	import { onMount } from 'svelte';

	export let postId: string | undefined = undefined;
	export let query: BlogItemQuery;
	$: data = query?.post ?? {
		postId: 0,
		createTime: '',
		title: 'Not Found',
		summaryHtml: '',
		contentHtml: '',
		tags: []
	};
	$: recentPosts = query?.recentPosts ?? [];

	async function fetchPost(post: string) {
		try {
			const res = await fetch(`${BASE_URL}/api/content/post/${post}`, {
				credentials: 'include'
			});
			const json: BlogItemQuery = await res.json();

			if (res.ok) {
				query = json;
			}
		} catch (err) {}
	}

	onMount(() => {
		let queryParams = new URLSearchParams(window.location.search);
		const queryPostId = queryParams.get('i');
		if (postId !== queryPostId && queryPostId) {
			fetchPost(queryPostId);
		}
	});
</script>

<div class="readcol">
	<div />
	<div class="readblock">
		<article class="content">
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
	<div style="display: flex; align-items: start;">
		<div class="box">
			<h3 class="title is-4">Recent Posts</h3>
			<ul>
				{#each recentPosts as post}
					<li>
						<a href={`/blog/post?i=${post.postId}`}>
							{post.title}
						</a>
					</li>
				{/each}
			</ul>
		</div>
	</div>
</div>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/elements/title';
	@import 'bulma/sass/grid/columns';
	@import 'bulma/sass/elements/box';

	.content {
		line-break: anywhere;
	}

	hr {
		background-color: #2a392f;
	}
</style>
