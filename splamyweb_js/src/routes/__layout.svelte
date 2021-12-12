<script lang="ts">
	import Header from '$lib/Header.svelte';
	import '$lib/css/style.scss';
</script>

<Header />

<section class="section">
	<slot />
</section>

<style lang="scss">
	.section {
		padding-top: 3.25rem + 3;
	}
</style>
