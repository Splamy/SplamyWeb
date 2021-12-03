<script lang="ts">
import { mdiRocket } from '@mdi/js';

	import { onMount } from 'svelte';
	import Icon from './Icon.svelte';

	export let color: number = 0;

	let elem: HTMLDivElement;
	let x = window.innerWidth / 2;
	let y = window.innerHeight / 2;
	let angle = Math.random() * Math.PI * 2;
	let angleVel = ranomdAngleVel();
	let lastTimeStamp = performance.now();

	function mathMod(n: number, m: number) {
		return ((n % m) + m) % m;
	}

	function ranomdAngleVel() { return (Math.random() - 0.5) / 300; }

	function animate(time: DOMHighResTimeStamp) {
		if (!elem) return;
		let elapsed = time - lastTimeStamp;
		lastTimeStamp = time;
		angle += angleVel * elapsed;
		x += (Math.cos(angle) * (elapsed / 3)) ;
		y += (Math.sin(angle) * (elapsed / 3)) ;

		if (x < 0 || y < 0 || x > window.innerWidth || y > window.innerHeight) {
			angleVel = ranomdAngleVel();
			x = mathMod(x, window.innerWidth);
			y = mathMod(y, window.innerHeight);
		}

		elem.style.transform = `translate(${x}px, ${y}px) rotate(${angle + Math.PI / 2}rad)`;
		requestAnimationFrame(animate);
	}

	onMount(() => {
		requestAnimationFrame(animate);
	});
</script>

<div style="position:fixed;top:0;left:0;pointer-events: none;">
	<div bind:this={elem} style="color: hsl({color}, 50%, 50%);">
		<Icon path={mdiRocket} />
	</div>
</div>
