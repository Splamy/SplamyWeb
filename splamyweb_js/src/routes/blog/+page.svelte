<script lang="ts">
	import SummaryView from '$lib/blog/SummaryView.svelte';
	import Icon from '$lib/Icon.svelte';
	import { mdiRss } from '@mdi/js';
	import type { PageData } from './$types';

	export let data: PageData;
	$: posts = data.query?.posts ?? [];
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
