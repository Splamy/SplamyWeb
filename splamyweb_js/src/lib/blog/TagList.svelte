<script lang="ts">
	export let tags: string[];
	$: displayedTags = (tags ?? []).slice(0, 3);

	function colorHash(str: string): string {
		let hash = 0;
		for (let i = 0; i < str.length; i++) {
			hash = str.charCodeAt(i) + ((hash << 5) - hash);
		}
		return `hsl(${hash % 360}, 25%, 25%)`;
	}
</script>

<div class="tags">
	{#each displayedTags as tag}
		<a
			href="/blog?tags={encodeURIComponent(tag)}"
			class="tag"
			style="color: #CCC; background-color:{colorHash(tag)}">{tag}</a
		>
	{/each}
</div>

<style lang="scss">
	@import 'bulma/sass/elements/tag';

	.tag {
		color: rgb(200, 200, 200);

		// &:nth-child(-n + 4) {
		// 	display: block;
		// }
	}
</style>
