<script lang="ts">
	import type { TabStatsHeader } from '$lib/api';
	import Icon from '$lib/Icon.svelte';
	import { BASE_URL } from '$lib/util';
	import { mdiDotNet, mdiDownload, mdiLinux, mdiMicrosoft, mdiWeatherNight } from '@mdi/js';
	import type { PageData } from './$types';

	export let data: PageData = {
		stat_downloads : 'did',
		stat_instances : 'you',
		stat_bots : 'disable',
		stat_playtime : 'javascript?',
	};

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
			'.NET 6',
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
							<p class="title">{data.stat_downloads}</p>
						</div>
					</div>
					<div class="level-item has-text-centered">
						<div>
							<p class="heading">Instances</p>
							<p class="title">{data.stat_instances}</p>
						</div>
					</div>
					<div class="level-item has-text-centered">
						<div>
							<p class="heading">Bots</p>
							<p class="title">{data.stat_bots}</p>
						</div>
					</div>
					<div class="level-item has-text-centered">
						<div>
							<p class="heading">Playtime</p>
							<p class="title">{data.stat_playtime}</p>
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
									<a href="/nightly" class="button is-primary is-rounded is-small">
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
							href="https://github.com/Splamy/TS3AudioBot#install">#Install</a
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
								>CommandSystem</a
							>
						</li>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/Plugins">Plugins</a
							>
						</li>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/WebAPI">Web API</a
							>
						</li>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/Rights">Rights</a
							>
						</li>
						<li>
							<a rel="external" href="https://github.com/Splamy/TS3AudioBot/wiki/FAQ"
								>FAQ</a
							>
						</li>
						<li>
							<a
								rel="external"
								href="https://github.com/Splamy/TS3AudioBot/wiki/Changelog"
								>Changelog</a
							>
						</li>
					</ul>
				</article>
			</div>
			<div class="tile is-parent">
				<article class="tile is-child notification is-info">
					<p class="title">Swagger/OpenApi</p>
					<p class="subtitle">
						<a rel="external" href="https://tab.splamy.de/openapi/index.html"
							>Check out our live Swagger/OpenApi documentation</a
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
	@import '../../../lib/css/_prelude';
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
