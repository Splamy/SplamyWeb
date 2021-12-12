<script lang="ts">
	import RenderedTextEditor from '$lib/RenderedTextEditor.svelte';

</script>

<div>
	<label for="post_visible">Post visible</label>
	<input type="checkbox" id="post_visible" />
	<br />
	<RenderedTextEditor raw="" />
	<button class="button">Submit</button>
</div>
