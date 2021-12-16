<script context="module" lang="ts">
	import type { Load } from '@sveltejs/kit';
	import { prerendering } from '$app/env';

	export const load: Load = async ({ fetch }) => {
		if (prerendering) return {};
		const res = await fetch(`${BASE_URL}/api/tab/stats/header`);
		const json: TabStatsHeader = await res.json();

		if (res.ok) {
			return {
				props: {
					stat_downloads: json.downloads,
					stat_instances: json.runningInstances,
					stat_bots: json.runningBots,
					stat_playtime: json.playbackTime
				}
			};
		}

		return { status: res.status };
	};
</script>

<script lang="ts">
	import type { TabStatsHeader } from '$lib/api';
	import Icon from '$lib/Icon.svelte';
	import { BASE_URL } from '$lib/util';
	import { mdiDotNet, mdiDownload, mdiLinux, mdiMicrosoft, mdiWeatherNight } from '@mdi/js';

	export let stat_downloads: string = 'did';
	export let stat_instances: string = 'you';
	export let stat_bots: string = 'disable';
	export let stat_playtime: string = 'javascript?';

	// [Icon, Name, donwload name, URL stable, URL preview]
	let table: [string, string, string, string, string][] = [
		[
			mdiDotNet,
			'dotnet core 3.1',
			'TS3AudioBot.zip',
			'/api/nightly/projects/ts3ab/master/download',
			'❌'
		],
		[
			mdiDotNet,
			'.NET 5',
			'TS3AudioBot.zip',
			'Soon™',
			'/api/nightly/projects/ts3ab/develop/download'
		],
		[
			mdiLinux,
			'Linux x64',
			'TS3AudioBot.tar.gz',
			'/api/nightly/projects/ts3ab/master_linux_x64/download',
			'/api/nightly/projects/ts3ab/develop_linux_x64/download'
		],
		[
			mdiMicrosoft,
			'Windows x64',
			'TS3AudioBot.zip',
			'/api/nightly/projects/ts3ab/master_win_x64/download',
			'/api/nightly/projects/ts3ab/develop_win_x64/download'
		]
	];

	function is_url(text: string): boolean {
		return text.startsWith('http') || text.startsWith('/');
	}
</script>

<svelte:head>
	<title>TS3AudioBot</title>
</svelte:head>

<section class="hero section">
	<div class="hero-body">
		<div class="container" style="text-align: center;">
			<h2 class="title heading is-1" style="color:white;">TS3Audiobot</h2>
			<h3 class="subtitle" style="color:white;">
				Open source selfhosted music bot and more...
			</h3>
		</div>
	</div>
</section>

<section class="readblock">
	<div class="tile is-ancestor is-vertical">
		<div class="tile is-parent">
			<article class="tile is-child notification is-primary">
				<div class="level">
					<div class="level-item has-text-centered">
						<div>
							<p class="heading">Downloads</p>
							<p class="title">{stat_downloads}</p>
						</div>
					</div>
					<div class="level-item has-text-centered">
						<div>
							<p class="heading">Instances</p>
							<p class="title">{stat_instances}</p>
						</div>
					</div>
					<div class="level-item has-text-centered">
						<div>
							<p class="heading">Bots</p>
							<p class="title">{stat_bots}</p>
						</div>
					</div>
					<div class="level-item has-text-centered">
						<div>
							<p class="heading">Playtime</p>
							<p class="title">{stat_playtime}</p>
						</div>
					</div>
				</div>
			</article>
		</div>

		<div class="tile is-parent">
			<article class="tile is-child box">
				<p class="title">Downloads</p>
				<div class="content">
					<table class="table">
						<!-- <thead>
							<tr><th /> <th>Master</th> <th>Develop</th></tr>
						</thead> -->
						<tbody>
							{#each table as [icon, name, dlname, stable, preview]}
								<tr>
									<td class="ccol"
										><span><Icon path={icon} addclass="padr" />{name}</span></td
									>
									<td class="ccol">
										{#if is_url(stable)}
											<a
												rel="external"
												href="{BASE_URL}{stable}"
												download={dlname}
												class="button is-primary is-rounded is-small"
											>
												<Icon path={mdiDownload} />
												<span>Stable</span>
											</a>
										{:else}{stable}{/if}
									</td>
									<td class="ccol">
										{#if is_url(preview)}
											<a
												rel="external"
												href="{BASE_URL}{preview}"
												download={dlname}
												class="button is-primary is-rounded is-small"
											>
												<Icon path={mdiDownload} />
												<span>Experimental</span>
											</a>
										{:else}{preview}{/if}
									</td>
								</tr>
							{/each}
							<tr>
								<td colspan="3">
									For all available downloads check our nightly page
									<a href="/" class="button is-primary is-rounded is-small">
										<Icon path={mdiWeatherNight} />
									</a>
								</td>
							</tr>
						</tbody>
					</table>
					<br />
					<span>
						For installation instructions follow our Readme: <a
							rel="external"
							href="https://github.com/Splamy/TS3AudioBot#install"
							target="_blank">#Install</a
						>
					</span>
				</div>
			</article>
		</div>

		<div class="tile">
			<div class="tile is-parent is-vertical">
				<article class="tile is-child notification is-primary">
					<p class="title">Wiki</p>
					<ul>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/CommandSystem"
								target="_blank">CommandSystem</a
							>
						</li>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/Plugins"
								target="_blank">Plugins</a
							>
						</li>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/WebAPI"
								target="_blank">Web API</a
							>
						</li>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/Rights"
								target="_blank">Rights</a
							>
						</li>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/FAQ"
								target="_blank">FAQ</a
							>
						</li>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/Changelog"
								target="_blank">Changelog</a
							>
						</li>
					</ul>
				</article>
			</div>
			<div class="tile is-parent">
				<article class="tile is-child notification is-info">
					<p class="title">Swagger/OpenApi</p>
					<p class="subtitle">
						<a
							rel="external"
							href="https://tab.splamy.de/openapi/index.html"
							target="_blank">Check out our live Swagger/OpenApi documentation</a
						>
					</p>
					<figure class="image is-4by3">
						<img src={'/TS3AB_Swagger.png'} />
					</figure>
				</article>
			</div>
		</div>
	</div>
</section>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/elements/title';
	@import 'bulma/sass/layout/hero';
	@import 'bulma/sass/elements/content';
	@import 'bulma/sass/elements/notification';
	@import 'bulma/sass/elements/button';
	@import 'bulma/sass/components/level';
	@import 'bulma/sass/elements/box';
	@import 'bulma/sass/grid/tiles';
	@import 'bulma/sass/elements/container';
	@import 'bulma/sass/layout/section';

	.ccol {
		vertical-align: middle !important;
		> span {
			display: flex;
		}
	}
</style>
