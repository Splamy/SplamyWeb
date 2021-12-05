<script lang="ts">
	import { mdiCookie } from '@mdi/js';
	import Icon from '$lib/Icon.svelte';
	import type { CollectableData } from './minigame';

	export let coll: CollectableData;
	let x = coll.position.x;
	let y = coll.position.y;
</script>

<div style="position:fixed;top:0;left:0;pointer-events: none;">
	<div style="width:0; height:0; color: chocolate; transform: translate({x}px, {y}px)">
		<Icon path={mdiCookie} style={"transform: translate(-12px,-12px);"} />
	</div>
</div>
