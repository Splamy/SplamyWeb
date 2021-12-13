<script lang="ts">
	export let tags: string[] = [];

	$: tagsAsText = tags.join(', ');

	function onChange(this: HTMLInputElement) {
		let tagSplit = this.value.split(/\s*,\s*/);
		const duplicateTags = new Set();
		for (let i = 0; i < tagSplit.length; i++) {
			const tag = tagSplit[i];
			if (duplicateTags.has(tag)) {
				tagSplit.splice(i, 1);
				i--;
				continue;
			}
			duplicateTags.add(tag);
		}
		tags = tagSplit;
	}
</script>

<input class="input" type="text" value={tagsAsText} on:change={onChange} placeholder="Tags" />
