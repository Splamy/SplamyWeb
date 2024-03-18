<script lang="ts">
	import { BASE_URL } from '$lib/util';
	import Monaco from '$lib/Monaco.svelte';

	export let queryTitle: string;
	export let queryType: string;
	export let queryExample: string = "";

	let query_json_contains = queryExample;
	let result_json_contains = '';

	async function runQueryJson(download: boolean) {
		let result = '';

		try {
			const mapsResult = await run_query(query_json_contains);
			console.log(mapsResult);
			throw_if_error(mapsResult);
			mapsResult.count = mapsResult.maps.length;
			result = JSON.stringify(mapsResult, null, '\t');
			if (download) {
				run_download(mapsResult.maps);
			}
		} catch (err) {
			result = `Error while fetching data: ${err}`;
		}
		result_json_contains = result;
	}

	async function run_query(query: string): Promise<{ maps: string[] } | { error: string }> {
		const resp = await fetch(`${BASE_URL}/api/ramses/query/${queryType}`, {
			credentials: 'include',
			method: 'POST',
			body: query,
			headers: {
				'Content-Type': 'application/json'
			}
		});

		if (!resp.ok) {
			const result = await resp.text();
			return { error: result };
		}

		return await resp.json();
	}

	async function run_download(maps: string[]) {
		const dlLink = `${BASE_URL}/api/ramses/download/${maps.join(',')}`;

		const a = document.createElement('a');
		a.href = dlLink;
		a.download = 'maps.tar.gz';
		a.click();
	}

	function throw_if_error(result: { error?: string }) {
		if (result.error) {
			throw new Error(result.error);
		}
		return result;
	}
</script>

<form class="tile is-child box">
	<div class="field">
		<label class="label" for="fld_version">{queryTitle}</label>
		<div class="control" style="min-height: 10em; display: flex;">
			<Monaco bind:content={query_json_contains} />
		</div>
	</div>

	<div class="field">
		<div class="control">
			<button type="submit" class="button is-primary" on:click={() => runQueryJson(false)}
				>Query</button
			>
			<button type="submit" class="button is-primary" on:click={() => runQueryJson(true)}
				>Download</button
			>
		</div>
	</div>

	<div class="field is-hidden">
		<div class="control" style="min-height: 10em; display: flex;">
			<Monaco content={result_json_contains} readOnly={true} />
		</div>
	</div>
</form>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/form/shared';
	@import 'bulma/sass/form/tools';
	@import 'bulma/sass/elements/box';
	@import 'bulma/sass/elements/button';
	@import 'bulma/sass/grid/tiles';
</style>
