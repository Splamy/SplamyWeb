<script lang="ts">
	import { page } from '$app/stores';
	import { enhance } from '$lib/form';
	import Icon from '$lib/Icon.svelte';
	import { applyLoginResult, CurrentUser } from '$lib/user';
	import { BASE_URL } from '$lib/util';
	import { mdiGithub, mdiTranslate } from '@mdi/js';
</script>

<nav>
	<a class="navl" href="/">Home</a>
	<a class="navl" href="/nightly">Nightly</a>
	<div class="navdrop">
		<a class="navl" href="/tsaudiobot/home">TS3AudioBot</a>
		<div class="navdrop-list">
			<a class="navl" href="/tsaudiobot/languagePacks">
				<Icon path={mdiTranslate} addclass="padr" />
				<span>Language Packs</span>
			</a>
			<!-- <a class="navl" href="/TSAudioBot/Stats">
				<span class="icon is-medium"><i class="mdi mdi-24px mdi-counter" /></span>
				Stats
			</a> -->
			<a
				class="navl"
				rel="external"
				href="https://github.com/Splamy/TS3AudioBot"
				target="_blank"
			>
				<Icon path={mdiGithub} addclass="padr" />
				<span>Github</span>
			</a>
		</div>
	</div>
	<a class="navl" href="/impress">Impress &amp; Privacy</a>
	{#if $CurrentUser}
		<a class="navl" href="/blog">Blog</a>
		<!-- <span class="icon is-medium"><i class="mdi mdi-24px mdi-calendar-text" /></span> -->
		<div class="navdrop">
			<a class="navl" href="/admin/hub">Admin</a>
			<div class="navdrop-list">
				<a class="navl" href="/admin/log">Log</a>
				<a class="navl" href="/admin/store">Store</a>
			</div>
		</div>
		<a class="navl navuser" href="/user/profile">User: {$CurrentUser.name}</a>
		<form
			action="{BASE_URL}/account/logout"
			method="POST"
			use:enhance={{
				result: () => {
					applyLoginResult(null);
				}
			}}
		>
			<label class="navl">
				Logout
				<input style="display: none" type="submit" />
			</label>
		</form>
	{:else}
		<a class="navl navuser" href="/user/login?return={$page.path}">Login</a>
	{/if}
</nav>

<style lang="scss">
	@import './css/_preset';
	@import './css/_util';

	nav {
		display: flex;
		flex-wrap: wrap;
		background-color: $primary;

		.navl {
			display: flex;
			padding: 14px 16px;
			color: $primary-invert;
			text-decoration: none;
			cursor: pointer;
			align-items: center;
		}

		.navl:hover {
			@include link_shine;
		}

		.navuser {
			margin-left: auto;
		}

		.navdrop {
			@include unselectable;
			position: relative;
			display: inline-block;
		}

		.navdrop-list {
			display: none;
			position: absolute;
			background-color: $primary;
			z-index: 1;
			white-space: nowrap;
		}

		.navdrop:hover .navdrop-list {
			display: block;
		}
	}
</style>
