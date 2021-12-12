<script lang="ts">
	import type { BlogViewData } from '$lib/api';
	import SummaryView from '$lib/blog/SummaryView.svelte';

	export let blogViews: BlogViewData[];
</script>

<div class="blogbox">
	{#each blogViews as blogView}
		<SummaryView {blogView} />
	{/each}
</div>

<style lang="scss">
	.blogbox {
		width: 100%;
		max-width: 1200px;
		margin: auto;

		:global {
			.blogentry {
				max-width: 800px;
				&:nth-child(even) {
					margin-left: auto;

					h2 {
						text-align: right;
					}
				}
			}
		}
	}
</style>
