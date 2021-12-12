<script lang="ts">
	import { mdiRocket } from '@mdi/js';
	import { onMount } from 'svelte';
	import Icon from '$lib/Icon.svelte';
	import type { Rocket } from './minigame';

	export let rocket: Rocket;

	let elem: HTMLDivElement;
	let update = rocket.update;
	let display = '';

	$: {
		let _ = $update;
		if (rocket.name) {
			display = `${rocket.name}\n${rocket.points}pts`;
		}
	}

	onMount(() => {
		rocket.elem = elem;
	});
</script>

<div class="zoomie">
	<div bind:this={elem} style="position: relative; color: hsl({rocket.color}, 50%, 50%);">
		<div class="rocket" style="width:0; height:0;">
			<Icon path={mdiRocket} style={'transform: translate(-12px,-12px);'} />
		</div>
		<span class="name" style="position: absolute; left: 20px">{display}</span>
	</div>
</div>

<style>
	.zoomie {
		position: fixed;
		top: 0;
		left: 0;
		pointer-events: none;
		z-index: 100;
	}
</style>
