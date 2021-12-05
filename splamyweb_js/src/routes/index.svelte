<script lang="ts">
	import Icon from '$lib/Icon.svelte';
	import MiniGame from '$lib/mini/MiniGame.svelte';
	import { mdiRobotHappy } from '@mdi/js';
</script>

<svelte:head>
	<title>Home</title>
</svelte:head>

<h1 class="title">Splamy</h1>

<h2>Home!</h2>
<br />

<span style="cursor: pointer;">
	<Icon path={mdiRobotHappy} addclass="rpad" /> woking on stuff...</span
>

<MiniGame />
