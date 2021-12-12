<script context="module" lang="ts">
	import { BASE_URL } from '$lib/util';
	import type { BlogViewData } from '$lib/api';
	import type { Load } from '@sveltejs/kit';
	import { prerendering } from '$app/env';

	export const load: Load = async ({ fetch }) => {
		if (prerendering) return {};
		const res = await fetch(`${BASE_URL}/api/content/posts`);
		const json: BlogViewData[] = await res.json();

		if (res.ok) {
			return {
				props: {
					blogViews: json
				}
			};
		}

		return { status: res.status };
	};
</script>

<script lang="ts">
	import SummaryView from '$lib/blog/SummaryView.svelte';

	export let blogViews: BlogViewData[] = [];
</script>

<svelte:head>
	<title>Blog</title>
</svelte:head>

<div class="readblock">
	{#each blogViews as blogView}
		<SummaryView {blogView} />
	{/each}
</div>

<style lang="scss">
</style>
