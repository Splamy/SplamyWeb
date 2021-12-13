<script lang="ts">
	import { page } from '$app/stores';
	import Icon from '$lib/Icon.svelte';
	import { applyLoginResult, CurrentUser } from '$lib/user';
	import { BASE_URL, enhance } from '$lib/util';
	import { mdiChartBar, mdiGithub, mdiTranslate, mdiWrench } from '@mdi/js';
</script>

<nav>
	<a class="navl" href="/">Home</a>
	<a class="navl" href="/nightly">Nightly</a>
	<div class="navdrop" tabindex="0">
		<a class="navl" href="/tsaudiobot/home">TS3AudioBot</a>
		<div class="navdrop-list">
			<a class="navl" href="/tsaudiobot/languagePacks">
				<Icon path={mdiTranslate} addclass="padr" />
				<span>Language Packs</span>
			</a>
			<a
				class="navl"
				rel="external"
				href="https://github.com/Splamy/TS3AudioBot"
				target="_blank"
			>
				<Icon path={mdiGithub} addclass="padr" />
				<span>Github</span>
			</a>
			<a class="navl" href="/ts/version">
				<Icon path={mdiWrench} addclass="padr" />
				<span>TS Version Checker Tool</span>
			</a>
			{#if $CurrentUser}
				<a class="navl" href="/tsaudiobot/stats">
					<Icon path={mdiChartBar} addclass="padr" />
					<span>Stats</span>
				</a>
			{/if}
		</div>
	</div>
	<div class="navdrop" tabindex="0">
		<a class="navl" href="/blog">Blog</a>
		{#if $CurrentUser}
			<div class="navdrop-list">
				<a class="navl" href="/blog/editor">
					<span>Write</span>
				</a>
			</div>
		{/if}
	</div>
	{#if $CurrentUser}
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
	@import './css/_prelude';
	@import './css/_util';

	nav {
		display: flex;
		flex-wrap: wrap;
		background-color: $primary;
	}

	.navl {
		display: flex;
		padding: 14px 16px;
		color: $primary-invert !important;
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

	.navdrop:hover,
	.navdrop:focus {
		.navdrop-list {
			display: block;
		}
	}
</style>
