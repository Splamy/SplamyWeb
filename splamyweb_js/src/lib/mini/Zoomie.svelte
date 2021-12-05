<script lang="ts">
	import { mdiRocket } from '@mdi/js';
	import { onMount } from 'svelte';
	import Icon from '$lib/Icon.svelte';
	import type { Rocket } from './minigame';

	export let rocket: Rocket;

	let elem: HTMLDivElement;
	let update = rocket.update;
	let points = 0;

	$: {
		let _ = $update;
		points = rocket.points;
	}

	onMount(() => {
		rocket.elem = elem;
	});
</script>

<div style="position:fixed;top:0;left:0;pointer-events: none;">
	<div bind:this={elem} style="position: relative; color: hsl({rocket.color}, 50%, 50%);">
		<div class="rocket" style="width:0; height:0;">
			<Icon path={mdiRocket} style={"transform: translate(-12px,-12px);"} />
		</div>
		<span class="name" style="position: absolute; left: 20px">{`${rocket.name}\n${points}pts`}</span>
	</div>
</div>
