<script lang="ts">
	type Color = 'b' | 'r' | '_'; // Blue, Red, or Default (no color)
	type Direction = 'ul' | 'u' | 'ur' | 'l' | 'x' | 'r' | 'dl' | 'd' | 'dr' | '_';

	export let block = "_";
	let color: 'b' | 'r' | '_' = block === '_' ? '_' : block[0] as Color;
	let dir: Direction = block === '_' ? '_' : block.slice(1) as Direction;

	$: block = color !== '_' && dir !== '_' ? `${color}${dir}` : "_";

	function setDirection(event: Event) {
		const target = event.target as HTMLImageElement;
		let selectDir = target.dataset.direction as Direction;

		if (selectDir === dir) {
			if (color === '_') {
				color = 'b';
			} else if (color === 'b') {
				color = 'r';
			} else {
				color = '_'; // Reset to default if the same color is clicked
				dir = '_'; // Reset direction as well
			}
		} else {
			dir = selectDir;
			if (color === '_') {
				color = 'b'; // Default to blue if no color is set
			}
		}
	}

	function getColor(direction: Direction, c: Color): string {
		if (direction === dir) {
			return c === 'b' ? 'blue' : 'red';
		} else {
			return 'blue';
		}
	}

	let gridData: { direction: Direction; rotation: string }[] = [
		{ direction: 'ul', rotation: '135deg' },
		{ direction: 'u', rotation: '180deg' },
		{ direction: 'ur', rotation: '225deg' },
		{ direction: 'l', rotation: '90deg' },
		{ direction: 'x', rotation: '0deg' },
		{ direction: 'r', rotation: '270deg' },
		{ direction: 'dl', rotation: '45deg' },
		{ direction: 'd', rotation: '0deg' },
		{ direction: 'dr', rotation: '315deg' }
	];
</script>

<div class="grid">
	{#each gridData as { direction, rotation }}
		<img
			src="https://raw.githubusercontent.com/laugexd/beat-saber-assets/refs/heads/master/icons/notes/{getColor(
				direction,
				color
			)}-{direction === 'x' ? 'nondirectional' : 'directional'}.svg"
			on:keypress={setDirection}
			on:click={setDirection}
			data-direction={direction}
			alt="Hit direction {direction}"
			style={`transform: rotate(${rotation});`}
			class:gray={color === '_' && direction !== dir}
			class:hide={color !== '_' && direction !== dir}
			class:grid-fill={color !== '_' && direction === dir}
		/>
	{/each}
</div>

<style>
	/* CSS Grid Template with named regions */
	.grid {
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		grid-template-rows: repeat(3, 1fr);
		grid-template-areas:
			'ul u ur'
			'l  x r'
			'dl d dr';
		gap: 0px;
	}

	.hide {
		display: none;
	}

	.grid-fill {
		grid-column: 1 / -1;
		grid-row: 1 / -1;
	}

	.gray {
		filter: grayscale(100%);
	}
</style>
