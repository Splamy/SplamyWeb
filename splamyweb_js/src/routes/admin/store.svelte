<script context="module" lang="ts">
	import type { Load } from '@sveltejs/kit';
	import type { KeyValue } from '$lib/api';
	import { browser } from '$app/env';

	export const load: Load = async ({ fetch }) => {
		if (!browser) return {};
		const res = await fetch(`${BASE_URL}/api/store/all`, {
			credentials: 'include'
		});
		const kvpList = (await res.json()) as KeyValue[];

		if (res.ok) {
			return {
				props: {
					kvpList
				}
			};
		}

		return { status: res.status };
	};
</script>

<script lang="ts">
	import Icon from '$lib/Icon.svelte';
	import { BASE_URL } from '$lib/util';
	import { mdiContentSave, mdiDelete, mdiPlus } from '@mdi/js';

	export let kvpList: KeyValue[] = [];

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

	function auto_grow(this: HTMLElement) {
		this.style.height = '5px';
		this.style.height = this.scrollHeight + 'px';
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
		kvpList = (await res.json()) as KeyValue[];
	}

	function protectText(this: HTMLElement) {
		this.classList.add("protected");
	}

	function showText(this: HTMLElement) {
		this.classList.remove("protected");
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
		{#each kvpList as { key, value }}
			<tr>
				<td>
					<label class="label" for="key_{key}">{key}</label>
				</td>
				<td>
					<textarea
						id="key_{key}"
						autocomplete="off"
						class="input protected"
						type="text"
						on:mouseenter={showText}
						on:mouseleave={protectText}
						on:input={auto_grow}
						on:keypress={onenter(() => updateKey(key))}>{value}</textarea
					>
				</td>
				<td>
					<button class="button" on:click={() => updateKey(key)}>
						<Icon path={mdiContentSave} />
					</button>
					<button class="button" on:click={() => deleteKey(key)}>
						<Icon path={mdiDelete} />
					</button>
				</td>
			</tr>
		{/each}

		<tr>
			<td>
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
					type="text"
					placeholder="value"
					bind:value={new_kvp_value}
					on:input={auto_grow}
					on:keypress={onenter(() => createKey())}
				/>
			</td>
			<td>
				<button href="#" class="button" on:click={() => createKey()}>
					<Icon path={mdiPlus} />
				</button>
			</td>
		</tr>
	</tbody>
</table>

<style lang="scss">
	.protected {
		//filter: blur(2px);
	}
</style>
