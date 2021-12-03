<script lang="ts">
	import Icon from '$lib/Icon.svelte';
	import Zoomie from '$lib/Zoomie.svelte';
	import { mdiRobotHappy } from '@mdi/js';

	let zoomies = [];

	function add() {
		zoomies.push(Math.round(Math.random() * 360));
		zoomies = zoomies;
	}
</script>

<svelte:head>
	<title>Home</title>
</svelte:head>

<h1 class="title">Splamy</h1>

<h2>Home!</h2>
<br />

<span style="cursor: pointer;" on:click={add}>
	<Icon path={mdiRobotHappy} addclass="rpad" /> woking on stuff...</span
>

{#each zoomies as zoomie}
	<Zoomie color={zoomie} />
{/each}
