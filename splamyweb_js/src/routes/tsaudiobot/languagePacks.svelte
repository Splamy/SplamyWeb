<script context="module" lang="ts">
	import type { Load } from '@sveltejs/kit';
	import { prerendering } from '$app/env';

	export const load: Load = async ({ fetch }) => {
		if (prerendering) return {};
		const res = await fetch(`${BASE_URL}/api/language/project/ts3ab/languages`, {
			credentials: 'include'
		});
		const langs = (await res.json()) as LangInfo[];

		if (res.ok) {
			return {
				props: {
					langs
				}
			};
		}

		return { status: res.status };
	};
</script>

<script lang="ts">
	import { BASE_URL } from '$lib/util';
	import type { LangInfo } from '$lib/api';
	import ShortDate from '$lib/ShortDate.svelte';

	export let langs: LangInfo[] = [];
</script>

<svelte:head>
	<title>TSAudioBot Language Packs</title>
</svelte:head>

<h1 class="title">TSAudioBot Language Packs</h1>

<article class="readblock">
	<p class="notification is-primary">
		<em>Want to help translate or improve translation?</em><br />
		Join us on
		<a rel="external" href="https://www.transifex.com/respeak/ts3audiobot/">Transifex</a>
		to help translate<br />
		or in our
		<a
			rel="external"
			href="https://gitter.im/TS3AudioBot/Lobby?utm_source=share-link&amp;utm_medium=link&amp;utm_campaign=share-link"
			>Gitter</a
		>
		to discuss or ask anything!<br />
	</p>

	<p class="notification is-info">
		Note: The TSAudioBot will automatically download language extension packs when you select
		the language.
	</p>

	<table class="table" style="width:100%;">
		<tr>
			<th>Language</th>
			<th>Built</th>
			<th>Link</th>
		</tr>

		{#each langs as lang}
			<tr>
				<td>{lang.displayName}</td>
				<td><ShortDate date={lang.uploadTime} /></td>
				<td
					><a
						rel="external"
						href="{BASE_URL}/api/language/project/ts3ab/language/{lang.language}/dll"
						download="TS3AudioBot.resources.dll">Download</a
					></td
				>
			</tr>
		{/each}
	</table>
</article>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/elements/title';
	@import 'bulma/sass/elements/table';
	@import "bulma/sass/elements/notification";
</style>
