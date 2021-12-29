<script lang="ts">
	import type { BlogPostShortView } from '$lib/api';
	import ShortDate from '$lib/ShortDate.svelte';
	import TagList from './TagList.svelte';

	export let post: BlogPostShortView;
</script>

<div class="columns is-size-7 is-gapless">
	<div class="column">
		Posted <ShortDate date={post.createTime} />
		{#if post.visible === false}
			<span class="tag is-warning">Hidden</span>
		{/if}
	</div>
	<div class="column is-narrow">
		<TagList tags={post.tags} />
	</div>
</div>

<style lang="scss">
	@import '../css/_prelude';
	@import 'bulma/sass/elements/tag';
	@import 'bulma/sass/grid/columns';
</style>
