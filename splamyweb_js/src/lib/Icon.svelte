<svelte:options immutable />

<script lang="ts">
	export let path: string;
	export let size: 18 | 24 | 36 | 48 | undefined = undefined;
	export let isLeft: boolean = false;
	export let isRight: boolean = false;
	export let addclass: string = '';
	export let style: string = "";
	export let spin: boolean = false;

	let mdiFontsize = '';
	if (size !== undefined) {
		mdiFontsize = ` font-size: ${size}px;`;
	}
</script>

<svg
	class="icon{isLeft ? ' is-left' : ''}{isRight ? ' is-right' : ''} {addclass}"
	class:spin
	viewBox="0 0 24 24"
	style="{mdiFontsize}{style}"
>
	<path d={path} />
</svg>

<style lang="scss">
	path {
		fill: currentColor;
	}

	.spin {
		-webkit-animation: mdi-spin 2s infinite linear;
		animation: mdi-spin 2s infinite linear;
	}
	@-webkit-keyframes mdi-spin {
		0% {
			-webkit-transform: rotate(0deg);
			transform: rotate(0deg);
		}
		100% {
			-webkit-transform: rotate(359deg);
			transform: rotate(359deg);
		}
	}
	@keyframes mdi-spin {
		0% {
			-webkit-transform: rotate(0deg);
			transform: rotate(0deg);
		}
		100% {
			-webkit-transform: rotate(359deg);
			transform: rotate(359deg);
		}
	}
</style>
