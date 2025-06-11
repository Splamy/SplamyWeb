<script lang="ts">
	import type { PageData } from './$types';
	import SearchPanel from './SearchPanel.svelte';
	import VisualPatternSearcher from './VisualPatternSearcher.svelte';

	export let data: PageData = {
		indexedSongs: '0',
		indexedDifficulties: '0',
		totalSize: '0'
	};

	const examplePattern = 'rdl bdl//\n_ _ rdr bdr//\n/rul/_ bul\n/_ _ _ bur/_ _ rur';
	let search_patter = examplePattern;
</script>

<svelte:head>
	<title>Beatsaber Metadata DB</title>
</svelte:head>

<h1 class="title">Beatsaber Metadata DB</h1>

<article class="section readblock">
	<div class="tile is-ancestor is-vertical">
		<article class="tile is-child notification is-primary">
			<div class="level">
				<div class="level-item has-text-centered">
					<div>
						<p class="heading">Indexed Songs</p>
						<p class="title">{data.indexedSongs}</p>
					</div>
				</div>
				<div class="level-item has-text-centered">
					<div>
						<p class="heading">Parsed Difficulties</p>
						<p class="title">{data.indexedDifficulties}</p>
					</div>
				</div>
				<div class="level-item has-text-centered">
					<div>
						<p class="heading">Compressed Data Size</p>
						<p class="title">{data.totalSize}</p>
					</div>
				</div>
			</div>
		</article>

		<div class="tile is-parent">
			<SearchPanel
				queryTitle="Query by contained json"
				queryType="json"
				query={'{\n\t"_songName": "Halloween Spooky Mash Up"\n}'}
			/>
		</div>
		<div class="tile is-parent">
			<SearchPanel
				queryTitle="Query by json expression"
				queryType="logic"
				query={'{"==" : [ { "var" : "_songName" }, "Halloween Spooky Mash Up" ]}'}
			/>
		</div>
		<div class="tile is-parent">
			<SearchPanel
				queryTitle="Search by pattern"
				queryType="pattern"
				language=""
				query={examplePattern}
			/>
		</div>

		<div class="tile is-parent">
			<SearchPanel
				queryTitle="Search by pattern with visual editor"
				queryType="pattern"
				language=""
				bind:query={search_patter}
				readOnly={true}
			>
				<VisualPatternSearcher bind:pattern={search_patter} />
			</SearchPanel>
		</div>
		<div class="tile is-parent"></div>
	</div>
</article>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/components/level';
	@import 'bulma/sass/grid/tiles';
	@import 'bulma/sass/layout/section';
</style>
