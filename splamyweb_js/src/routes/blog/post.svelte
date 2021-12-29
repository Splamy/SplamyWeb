<script context="module" lang="ts">
	import { BASE_URL } from '$lib/util';
	import { BlogItemQuery, BlogPostShortView, BlogPostView, EMPTY_POST } from '$lib/api';
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
	import { onMount } from 'svelte';
	import { CurrentUser } from '$lib/user';
	import PostView from '$lib/blog/PostView.svelte';

	export let postId: string | undefined = undefined;
	export let query: BlogItemQuery;

	let data: BlogPostView;
	let recentPosts: BlogPostShortView[];

	$: {
		data = query?.post ?? EMPTY_POST();
		recentPosts = query?.recentPosts ?? [];
	}

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
	<PostView {data} />
	<div class="sidebar">
		<div class="sticky">
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
			{#if $CurrentUser}
				<div class="box">
					<a href="/blog/editor?post={data.postId}">Edit</a>
				</div>
			{/if}
		</div>
	</div>
</div>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/elements/title';
	@import 'bulma/sass/elements/box';

	.sidebar {
		display: flex;
		align-items: start;
		flex-direction: column;

		> * {
			width: 100%;
			max-width: 20em;
			min-width: 10em;
		}
	}
</style>
