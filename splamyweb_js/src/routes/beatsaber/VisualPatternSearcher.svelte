<script lang="ts">
	import { writable } from 'svelte/store';
	import DragDropList from './DragDropList.svelte';
	import TimeframePicker from './TimeframePicker.svelte';
	import type { Writable } from 'svelte/store';
	import { text } from '@sveltejs/kit';

	export let pattern: string = '';

	let timeframes: Writable<{ pat: string; id: number }[]> = writable([]);

	$: {
		// console.log('Timeframes updated:', $timeframes);
		pattern = $timeframes.map((x) => x.pat).join('\n');
	}
</script>

<div style="margin-bottom: 2em;">
	<DragDropList bind:data={$timeframes} removesItems={true} let:index let:item>
		<TimeframePicker bind:pattern={$timeframes[index].pat} />
	</DragDropList>

	<button
		style="margin-top: 1em;"
		class="button is-primary"
		on:click={() => {
			const newId = $timeframes.length ? $timeframes[$timeframes.length - 1].id + 1 : 1;
			$timeframes = [... $timeframes, { pat: '//', id: newId }];
		}}>Add Timeframe</button
	>
</div>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/elements/button';
</style>
