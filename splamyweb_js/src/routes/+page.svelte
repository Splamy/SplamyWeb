<script lang="ts">
	import HomeView from '$lib/blog/HomeView.svelte';
	import Icon from '$lib/Icon.svelte';
	import { loadMinigame } from '$lib/mini/loader';
	import type MiniGame from '$lib/mini/MiniGame.svelte';
	import { mdiRobotHappy } from '@mdi/js';
	import { tick } from 'svelte';
	import type { PageData } from './$types';

	export let data: PageData;

	let minigameComponent: any = undefined;
	let minigame: MiniGame;

	async function init() {
		if (minigameComponent !== undefined) return;
		minigameComponent = await loadMinigame();
		await tick();
		for (let i = 0; i < 5; i++) {
			minigame.add();
		}
	}
</script>

<svelte:head>
	<title>Home</title>
</svelte:head>

<h1 class="title">Splamy</h1>

<HomeView query={data.query} />

<div class="easteregg">
	<span class="icon-text" style="cursor: pointer;" on:click={init}>
		<Icon path={mdiRobotHappy} addclass="padr" />
		working on more stuff...
	</span>
</div>

{#if minigameComponent}
	<svelte:component this={minigameComponent} bind:this={minigame} />
{/if}

<style lang="scss">
	@import "../lib/css/_prelude";
	@import "bulma/sass/elements/icon";

	.easteregg {
		padding-top: 3em;
		width: 100%;
		display: flex;
		flex-direction: column;
		align-items: center;
	}
</style>
