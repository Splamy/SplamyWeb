<script lang="ts">
	import { BASE_URL } from '$lib/util';
	import { CurrentUser } from '$lib/user';
	import Monaco from '$lib/Monaco.svelte';

	export let queryTitle: string;
	export let queryType: string;
	export let query: string = '';
	export let language: string = 'json';
	export let readOnly: boolean = false;

	let result_json_contains = '';
	let result_count = 0;

	function handleSubmit(event: Event) {
		event.preventDefault();
		const download = (event.target as HTMLElement).dataset.download === 'true';
		runQueryJson(download);
		return false;
	}

	async function runQueryJson(download: boolean) {
		let result = '';

		try {
			let jsonBody: string;
			if (queryType == 'pattern') {
				query = query.trim();
				if (!query.startsWith('["')) {
					jsonBody = JSON.stringify(query.split('\n'));
				} else {
					jsonBody = query;
				}
			} else {
				jsonBody = query;
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
			{#if readOnly}
				<Monaco content={query} readOnly={true} {language} />
			{:else}
				<Monaco bind:content={query} {language} />
			{/if}
		</div>
	</div>

	<slot />

	<div class="field">
		<div class="control">
			<button type="submit" class="button is-primary" on:click={handleSubmit}>Query</button>
			{#if $CurrentUser != null}
				<button
					type="submit"
					class="button is-primary"
					data-download="true"
					on:click={handleSubmit}>Download</button
				>
			{/if}
		</div>
	</div>

	<div class="field is-hidden">
		<div class="control" style="min-height: 10em; display: flex;">
			<Monaco content={result_json_contains} readOnly={true} language="json" />
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
