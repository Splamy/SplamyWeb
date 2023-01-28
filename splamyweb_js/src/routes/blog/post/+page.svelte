<script lang="ts">
	import { onMount } from 'svelte';
	import { CurrentUser } from '$lib/user';
	import PostView from '$lib/blog/PostView.svelte';
	import { BASE_URL } from '$lib/util';
	import { EMPTY_POST } from '$lib/api';
	import type { BlogItemQuery, BlogPostShortView, BlogPostView } from '$lib/api';
	import type { PageData } from './$types';

	export let data: PageData;
	let postId: string | undefined = data.postId;
	let query: BlogItemQuery = data.query;

	let post: BlogPostView;
	let recentPosts: BlogPostShortView[];

	$: {
		post = query?.post ?? EMPTY_POST();
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
	<PostView {post} />
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
					<a href="/blog/editor?post={post.postId}">Edit</a>
				</div>
			{/if}
		</div>
	</div>
</div>

<style lang="scss">
	@import '../../../lib/css/_prelude';
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
