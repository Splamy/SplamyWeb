<script lang="ts">
	import { BASE_URL } from '$lib/util';
	import { CurrentUser } from '$lib/user';
	import Monaco from '$lib/Monaco.svelte';

	export let queryTitle: string;
	export let queryType: string;
	export let queryExample: string = '';
	export let language: string = 'json';

	let query_json_contains = queryExample;
	let result_json_contains = '';
	let result_count = 0;

	async function runQueryJson(download: boolean) {
		let result = '';

		try {
			let jsonBody: string;
			if (queryType == 'pattern') {
				query_json_contains = query_json_contains.trim();
				if (!query_json_contains.startsWith('["')) {
					jsonBody = JSON.stringify(query_json_contains.split('\n'));
				} else {
					jsonBody = query_json_contains;
				}
			} else {
				jsonBody = query_json_contains;
			}

			const mapsResult = await run_query(jsonBody);
			console.log(mapsResult);
			if (is_ok(mapsResult)) {
				result_count = mapsResult.maps.length;
				result = JSON.stringify(mapsResult, null, '\t');
				if (download) {
					run_download(mapsResult.maps);
				}
			}
		} catch (err) {
			result = `Error while fetching data: ${err}`;
			result_count = 0;
		}
		result_json_contains = result;
	}

	type QueryOk = { maps: string[] };
	type QueryError = { error: string };

	async function run_query(query: string): Promise<QueryOk | QueryError> {
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
		const mapForm = document.createElement('form');
		mapForm.id = 'downloadBsMaps';
		mapForm.method = 'POST';
		mapForm.action = `${BASE_URL}/api/ramses/download`;

		const mapInput = document.createElement('input');
		mapInput.type = 'hidden';
		mapInput.name = 'keys';
		mapInput.value = maps.join(',');
		mapForm.appendChild(mapInput);
		document.body.appendChild(mapForm);

		mapForm.submit();
	}

	function is_ok(result: QueryOk | QueryError): result is QueryOk {
		if ('error' in result) {
			throw new Error(result.error);
		}
		return true;
	}
</script>

<form class="tile is-child box">
	<div class="field">
		<label class="label" for="fld_version">{queryTitle}</label>
		<div class="control" style="min-height: 10em; display: flex;">
			<Monaco bind:content={query_json_contains} language="json" />
		</div>
	</div>

	<div class="field">
		<div class="control">
			<button type="submit" class="button is-primary" on:click={() => runQueryJson(false)}
				>Query</button
			>
			{#if $CurrentUser != null}
				<button type="submit" class="button is-primary" on:click={() => runQueryJson(true)}
					>Download</button
				>
			{/if}
		</div>
	</div>

	<div class="field is-hidden">
		<div class="control" style="min-height: 10em; display: flex;">
			<Monaco content={result_json_contains} readOnly={true} {language} />
		</div>
	</div>

	{#if result_count > 0}
		<div class="field">
			<p class="help">Found {result_count} maps</p>
		</div>
	{/if}
</form>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/form/shared';
	@import 'bulma/sass/form/tools';
	@import 'bulma/sass/elements/box';
	@import 'bulma/sass/elements/button';
	@import 'bulma/sass/grid/tiles';
</style>
