<script lang="ts">
	import Icon from '$lib/Icon.svelte';
	import { autosize, BASE_URL } from '$lib/util';
	import { mdiContentSave, mdiDelete, mdiPlus } from '@mdi/js';
	import type { KeyValue } from '$lib/api';
	import type { PageData } from './$types';

	export let data: PageData;

	function onenter(func: () => void) {
		return (e: KeyboardEvent) => {
			if (e.key.toLowerCase() === 'enter') {
				if (e.shiftKey) {
					e.preventDefault();
					func();
				}
			}
		};
	}

	async function notify(elem: HTMLElement) {
		elem.classList.add('is-info');
		setTimeout(function () {
			elem.classList.remove('is-info');
		}, 500);
	}

	async function updateKey(key: string) {
		const box = document.getElementById('key_' + key) as HTMLInputElement;
		const value = encodeURIComponent(box.value);
		await fetch(`${BASE_URL}/api/store/value/${key}?value=${value}`, {
			credentials: 'include',
			method: 'PUT'
		});
		notify(box);
	}

	let new_kvp_id = '';
	let new_kvp_value = '';

	async function createKey() {
		if (!new_kvp_id) {
			return;
		}
		const value = encodeURIComponent(new_kvp_value);
		await fetch(`${BASE_URL}/api/store/value/${new_kvp_id}?value=${value}`, {
			credentials: 'include',
			method: 'PUT'
		});
		new_kvp_id = '';
		new_kvp_value = '';
		await refresh();
	}

	async function deleteKey(key: string) {
		await fetch(`${BASE_URL}/api/store/value/${key}`, {
			credentials: 'include',
			method: 'DELETE'
		});
		await refresh();
	}

	async function refresh() {
		const res = await fetch(`${BASE_URL}/api/store/all`, {
			credentials: 'include'
		});
		data.kvpList = (await res.json()) as KeyValue[];
	}

	function protectText(this: HTMLElement) {
		this.classList.add('protected');
	}

	function showText(this: HTMLElement) {
		this.classList.remove('protected');
	}
</script>

<svelte:head>
	<title>Store</title>
</svelte:head>

<table class="table" style="width: 100%">
	<thead>
		<tr>
			<th>Key</th>
			<th>Value</th>
			<th />
		</tr>
	</thead>
	<tbody>
		{#each data.kvpList as { key, value }}
			<tr>
				<td class="compact">
					<label class="label" for="key_{key}">{key}</label>
				</td>
				<td>
					<textarea
						id="key_{key}"
						autocomplete="off"
						class="input protected"
						type="text"
						use:autosize
						on:focus={showText}
						on:blur={protectText}
						on:keypress={onenter(() => updateKey(key))}>{value}</textarea
					>
				</td>
				<td class="compact">
					<div class="buttons" style="flex-wrap: nowrap;">
						<button class="button" on:click={() => updateKey(key)}>
							<Icon path={mdiContentSave} />
						</button>
						<button class="button" on:click={() => deleteKey(key)}>
							<Icon path={mdiDelete} />
						</button>
					</div>
				</td>
			</tr>
		{/each}

		<tr>
			<td class="compact">
				<input
					id="new_key"
					autocomplete="off"
					class="input"
					type="text"
					placeholder="key"
					bind:value={new_kvp_id}
					on:keyup={onenter(() => createKey())}
				/>
			</td>
			<td>
				<textarea
					id="new_value"
					autocomplete="off"
					class="input"
					placeholder="value"
					bind:value={new_kvp_value}
					use:autosize
					on:keypress={onenter(() => createKey())}
				/>
			</td>
			<td class="compact">
				<button class="button" on:click={() => createKey()}>
					<Icon path={mdiPlus} />
				</button>
			</td>
		</tr>
	</tbody>
</table>

<style lang="scss">
	@import '../../../lib/css/_prelude';
	@import 'bulma/sass/elements/table';
	@import 'bulma/sass/form/shared';
	@import 'bulma/sass/form/tools';
	@import 'bulma/sass/form/input-textarea';
	@import 'bulma/sass/elements/button';

	.protected {
		text-shadow: 0px 0px 10px lime;
		color: transparent;
	}

	.input {
		width: 100% !important;
	}

	.compact {
		width: 1px;
		white-space: nowrap;
	}
</style>
