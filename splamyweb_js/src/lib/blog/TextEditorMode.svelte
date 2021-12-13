<script lang="ts">
	import Icon from '../Icon.svelte';
	import { mdiEye, mdiFlipHorizontal, mdiPencil } from '@mdi/js';
	import { View } from './editor';

	export let view: View = View.Edit;
</script>

<div class="field has-addons">
	<p class="control">
		<button
			title="Source"
			class="button"
			on:click|preventDefault|stopPropagation={() => (view = View.Edit)}
		>
			<Icon path={mdiPencil} />
		</button>
	</p>
	<p class="control">
		<button
			title="Split view"
			class="button"
			on:click|preventDefault|stopPropagation={() => (view = View.Both)}
		>
			<Icon path={mdiFlipHorizontal} />
		</button>
	</p>
	<p class="control">
		<button
			title="Preview"
			class="button"
			on:click|preventDefault|stopPropagation={() => (view = View.Rendered)}
		>
			<Icon path={mdiEye} />
		</button>
	</p>
</div>
