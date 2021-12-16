<script lang="ts">
	import Header from '$lib/Header.svelte';
	import Footer from '$lib/Footer.svelte';
	import '$lib/css/style.scss';
</script>

<div class="layout">
	<Header />

	<div class="pagebody">
		<section class="section">
			<slot />
		</section>

		<Footer />
	</div>
</div>

<style lang="scss">
	@import "bulma/sass/layout/section";

	.layout {
		height: 100%;
		display: flex;
		flex-direction: column;
	}

	.pagebody {
		flex: 1;
		overflow-y: scroll;

		display: grid;
		grid-template-rows: 1fr auto;
		min-height: 100%;
	}
</style>
