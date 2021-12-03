<script lang="ts">
	import RenderedTextEditor from '$lib/RenderedTextEditor.svelte';
	import { CurrentUser } from '$lib/user';
	let blogs = [];

	function navigate() {}
</script>

<svelte:head>
	<title>Blog</title>
</svelte:head>

<div class="readblock">
	<div>
		{#if $CurrentUser}
			<label for="post_visible">Post visible</label>
			<input type="checkbox" id="post_visible" />
			<br />
			<RenderedTextEditor raw="" />
			<button class="button">Submit</button>
		{/if}

		{#each blogs as entry}
			<article>
				<h2>{entry.Title}</h2>
				<div>
					<!-- Admin Tools -->
					<a>Edit</a>
					<a>Delete</a>
				</div>
				<p>{entry.Summary}</p>
				<p>{@html entry.ContentHtml}</p>
				<hr />
				<label>Tags</label>
				<div>
					{#each entry.Tags as tag}
						<a>{tag}</a>
					{/each}
				</div>
				<a on:click={navigate}>Read more...</a>
			</article>
		{/each}
	</div>
</div>

<style lang="scss">
</style>
