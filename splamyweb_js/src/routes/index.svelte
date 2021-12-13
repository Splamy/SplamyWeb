<script context="module" lang="ts">
	import { BASE_URL } from '$lib/util';
	import type { BlogListQuery, BlogViewData } from '$lib/api';
	import type { Load } from '@sveltejs/kit';
	import { prerendering } from '$app/env';

	export const load: Load = async ({ fetch }) => {
		if (prerendering) return {};
		const res = await fetch(`${BASE_URL}/api/content/home`);
		const json: BlogListQuery = await res.json();

		if (res.ok) {
			return {
				props: {
					query: json
				}
			};
		}

		return { status: res.status };
	};
</script>

<script lang="ts">
	import HomeView from '$lib/blog/HomeView.svelte';
	import Icon from '$lib/Icon.svelte';
	import { loadMinigame } from '$lib/mini/loader';
	import type MiniGame from '$lib/mini/MiniGame.svelte';
	import { mdiRobotHappy } from '@mdi/js';
	import { tick } from 'svelte';

	export let query: BlogListQuery;

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

<HomeView {query} />

<div class="easteregg">
	<span class="icon-text" style="cursor: pointer;" on:click={init}>
		<Icon path={mdiRobotHappy} addclass="padr" />
		woking on more stuff...
	</span>
</div>

{#if minigameComponent}
	<svelte:component this={minigameComponent} bind:this={minigame} />
{/if}

<style lang="scss">
	.easteregg {
		padding-top: 3em;
		width: 100%;
		display: flex;
		flex-direction: column;
		align-items: center;
	}
</style>
