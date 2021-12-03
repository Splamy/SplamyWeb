<script lang="ts">
	import Icon from '$lib/Icon.svelte';
	import Zoomie from '$lib/Zoomie.svelte';
	import { mdiRobotHappy } from '@mdi/js';
	import { clearAllRockets, isOnline, onlineInit, Rocket, ZOOMIES } from './minigame';

	let isFunny = false;
	let username = '';

	function add() {
		isFunny = true;
		if (isOnline()) return;
		let rocket: Rocket = Rocket.createLocal();
		rocket.color = Rocket.randomColor();
		rocket.position = {x : window.innerWidth / 2, y: window.innerHeight / 2 };
		rocket.angle = Rocket.randomRotation();
		rocket.angleVel = Rocket.randomAngleVel();
		rocket.followTarget = false;

		ZOOMIES.update((z) => [...z, rocket]);
	}

	async function connectOnline() {
		clearAllRockets();
		await onlineInit();
	}
</script>

<span style="cursor: pointer;" on:click={add}>
	<Icon path={mdiRobotHappy} addclass="rpad" /> woking on stuff...</span
>

{#if isFunny}
	<div class="field is-horizontal">
		<!-- <input
			class="input"
			style="max-width: 10em;"
			type="text"
			bind:value={username}
			placeholder="Username"
		/> -->
		<button class="button" on:click={connectOnline}>Join Online</button>
	</div>
{/if}

{#each $ZOOMIES as zoomie (zoomie.trackId)}
	<Zoomie rocket={zoomie} />
{/each}
