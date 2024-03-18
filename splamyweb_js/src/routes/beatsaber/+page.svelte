<script lang="ts">
	import { BASE_URL } from '$lib/util';
	import SearchPanel from './SearchPanel.svelte';
</script>

<svelte:head>
	<title>Beatsaber Metadata DB</title>
</svelte:head>

<h1 class="title">Beatsaber Metadata DB</h1>

<article class="section readblock">
	<div class="tile is-ancestor is-vertical">
		<div class="tile is-parent">
			<SearchPanel
				queryTitle="Query by contained json"
				queryType="json"
				queryExample={'{\n\t"_songName": "Halloween Spooky Mash Up"\n}'}
			/>
		</div>
		<div class="tile is-parent">
			<SearchPanel
				queryTitle="Query by json expression"
				queryType="logic"
				queryExample={'{"==" : [ { "var" : "_songName" }, "Halloween Spooky Mash Up" ]}'}
			/>
		</div>
	</div>
</article>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/grid/tiles';
	@import 'bulma/sass/layout/section';
</style>
