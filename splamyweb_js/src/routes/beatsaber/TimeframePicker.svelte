<script lang="ts">
	import { writable } from 'svelte/store';
	import BlockPicker from './BlockPicker.svelte';
	import type { Writable } from 'svelte/store';

	export let pattern: string = '//';
	// console.log('Initial pattern:', pattern);

	let cells: Writable<string[]> = writable(new Array(12).fill('_'));

	function parsePattern(pattern: string): string[] {
		return pattern.split('/')
			.flatMap(row => {
				let cells = row.replace('_', " _ ").split(' ').map(cell => cell.trim());
				while (cells.length < 4) {
					cells.push('_'); // Fill with underscores if less than 4
				}
				return cells.slice(0, 4); // Ensure only 4 cells per row
			});
	}

	function format_row(row: string[]): string {
		while (row[row.length - 1] === '_') {
			row.pop(); // Remove trailing underscores
		}
		return row.map(cell => cell.trim()).join(' ');
	}

	$: {
		// console.log('Cells updated:', $cells);

		let top = format_row($cells.slice(0, 4));
		let middle = format_row($cells.slice(4, 8));
		let bottom = format_row($cells.slice(8, 12));
		pattern = `${bottom}/${middle}/${top}`;
	}
</script>

<div class="timeframe">
	{#each $cells as cell, i}
		<BlockPicker bind:block={$cells[i]} />
	{/each}
</div>

<style>
	.timeframe {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		grid-template-rows: repeat(3, 1fr);
		gap: 2px;

		padding: 5px;
		border: 2px solid gray;
		border-radius: 8px;
	}
</style>
