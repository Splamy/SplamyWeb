<script context="module" lang="ts">
	import { BASE_URL } from '$lib/util';
	import type { BlogListQuery } from '$lib/api';
	import type { Load } from '@sveltejs/kit';
	import { prerendering } from '$app/env';

	export const load: Load = async ({ fetch }) => {
		if (prerendering) return {};
		const res = await fetch(`${BASE_URL}/api/content/posts`, {
			credentials: 'include'
		});
		const json: BlogListQuery = await res.json();

		if (res.ok) {
			return {
				props: {
					query: json
				}
			};
		}

		return { status: res.status };
	};
</script>

<script lang="ts">
	import SummaryView from '$lib/blog/SummaryView.svelte';
	import Icon from '$lib/Icon.svelte';
	import { mdiRss } from '@mdi/js';

	export let query: BlogListQuery;
	$: posts = query?.posts ?? [];
</script>

<svelte:head>
	<title>Blog</title>
</svelte:head>

<div class="readcol">
	<div />
	<div class="readblock">
		{#each posts as post}
			<SummaryView {post} />
		{/each}
	</div>
	<div style="display: flex; align-items: start;">
		<div class="box">
			<a
				href="/api/content/feed/rss"
				rel="external"
				target="_blank"
				class="button"
				style="color: #f26522; border-color: #f26522;"
			>
				<Icon path={mdiRss} />
			</a>
		</div>
	</div>
</div>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/elements/button';
	@import 'bulma/sass/elements/box';
</style>
