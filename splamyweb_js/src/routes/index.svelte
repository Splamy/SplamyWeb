<script lang="ts">
	import Icon from '$lib/Icon.svelte';
	import { loadMinigame } from '$lib/mini/loader';
	import type MiniGame from '$lib/mini/MiniGame.svelte';
	import { mdiRobotHappy } from '@mdi/js';
	import { tick } from 'svelte';

	let minigameComponent: any = undefined;
	let minigame: MiniGame;

	async function init() {
		if (minigameComponent !== undefined) return;
		minigameComponent = await loadMinigame();
		await tick();
		for (let i = 0; i < 1; i++) {
			minigame.add();
		}
	}
</script>

<svelte:head>
	<title>Home</title>
</svelte:head>

<h1 class="title">Splamy</h1>

<h2>Home!</h2>
<br />

<span style="cursor: pointer;" on:click={init} >
	<Icon path={mdiRobotHappy} addclass="rpad" /> woking on stuff...</span
>

{#if minigameComponent}
	<svelte:component bind:this={minigame} this={minigameComponent}/>
{/if}
