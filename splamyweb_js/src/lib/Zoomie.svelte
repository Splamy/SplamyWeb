<script lang="ts">
	import { mdiRocket } from '@mdi/js';
	import { onMount } from 'svelte';
	import Icon from './Icon.svelte';
	import { addRocket, Rocket } from './minigame';

	export let rocket: Rocket;

	let elem: HTMLDivElement;

	onMount(() => {
		rocket.elem = elem;
		addRocket(rocket);
	});
</script>

<div style="position:fixed;top:0;left:0;pointer-events: none;">
	<div bind:this={elem} style="color: hsl({rocket.color}, 50%, 50%);">
		<span>{rocket.name ?? ""}</span>
		<Icon path={mdiRocket} />
	</div>
</div>
