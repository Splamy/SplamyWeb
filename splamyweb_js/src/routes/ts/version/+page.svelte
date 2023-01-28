<script lang="ts">
	import Icon from '$lib/Icon.svelte';

	import { BASE_URL } from '$lib/util';
	import { mdiOpenInNew } from '@mdi/js';

	let version: string = '';
	let platform: string = '';
	let sign: string = '';

	let resultMsg = '';
	let isErr = false;

	function checkFn(event) {
		checkResult();
		event.preventDefault();
		return false;
	}

	async function checkResult() {
		resultMsg = '';

		if (!version || !platform || !sign) {
			resultMsg = 'Please fill out all fields';
			isErr = true;
			return;
		}

		const encVersion = encodeURIComponent(version);
		const encPlatform = encodeURIComponent(platform);
		const encSign = encodeURIComponent(sign);

		try {
			const resp = await fetch(
				`${BASE_URL}/api/teamspeak/version/${encVersion}/${encPlatform}?sign=${encSign}`,
				{
					method: 'POST'
				}
			);
			const result = await resp.json();
			if (typeof result === 'string') {
				resultMsg = result;
				isErr = false;
				return;
			} else if ('error' in result) {
				resultMsg = result.error;
				isErr = true;
				return;
			}
		} catch {}

		resultMsg = 'The api seems down, try again later...';
		isErr = true;
	}
</script>

<svelte:head>
	<title>TS Version</title>
</svelte:head>

<h1 class="title">Teamspeak Version Checker</h1>

<article class="section readblock">
	<div class="tile is-ancestor is-vertical">
		<div class="tile is-parent">
			<form id="check_form" class="tile is-child box" on:submit={checkFn}>
				<div class="field">
					<label class="label" for="fld_version">Version</label>
					<div class="control">
						<input
							bind:value={version}
							id="fld_version"
							name="version"
							class="input"
							placeholder="e.g. '3.0.11 [Build: 1374563791]'"
						/>
					</div>
				</div>
				<div class="field">
					<label class="label" for="fld_platform">Platform</label>
					<div class="control">
						<input
							bind:value={platform}
							id="fld_platform"
							name="platform"
							class="input"
							placeholder="e.g. 'Windows'"
						/>
					</div>
				</div>
				<div class="field">
					<label class="label" for="fld_sign">Sign</label>
					<div class="control">
						<input
							bind:value={sign}
							id="fld_sign"
							name="sign"
							class="input"
							placeholder="e.g. 'hQCwiLP5f4GIcDG5KQ1T+CNFGqRxyw5MXCHE8KjWRIgkjCuGSryK4vpPy70EURH3blQ8TKrax8BEorHlpnpdAQ=='"
						/>
					</div>
				</div>

				<div class="field">
					<div class="control">
						<button type="submit" class="button is-primary" on:click={checkFn}
							>Validate</button
						>
					</div>
				</div>

				{#if resultMsg}
					<div class="field is-hidden">
						<div class:is-info={!isErr} class:is-danger={isErr} class="notification">
							{resultMsg}
						</div>
					</div>
				{/if}
			</form>
		</div>

		<div class="tile is-parent">
			<article class="tile is-child notification is-primary">
				<span>
					You can check out all collected versions
					<a
						class="icon-text"
						rel="external"
						href="https://github.com/ReSpeak/tsdeclarations/blob/master/Versions.csv"
					>
						here <Icon path={mdiOpenInNew} />
					</a>
				</span>
			</article>
		</div>
	</div>
</article>

<style lang="scss">
	@import '../../../lib/css/_prelude';
	@import 'bulma/sass/elements/icon';
	@import 'bulma/sass/form/shared';
	@import 'bulma/sass/form/tools';
	@import 'bulma/sass/form/input-textarea';
	@import 'bulma/sass/elements/notification';
	@import 'bulma/sass/elements/box';
	@import 'bulma/sass/elements/button';
	@import 'bulma/sass/grid/tiles';
	@import 'bulma/sass/layout/section';
</style>
