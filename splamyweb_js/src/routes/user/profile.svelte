<script context="module" lang="ts">
	import type { Load } from '@sveltejs/kit';
	import { CurrentUser, User } from '$lib/user';
	import { get } from 'svelte/store';

	// see https://kit.svelte.dev/docs#loading
	export const load: Load = async ({}) => {
		const user = prerendering ? User.DUMMY : get(CurrentUser);
		if (user == null) {
			return {
				status: 300,
				redirect: '/user/login'
			};
		} else {
			return {};
		}
	};
</script>

<script lang="ts">
	import { BASE_URL, enhance } from '$lib/util';
	import { goto } from '$app/navigation';
	import { browser, prerendering } from '$app/env';

	$: if (browser && $CurrentUser == null) goto('/user/login');

	let errors = [];
</script>

<svelte:head>
	<title>Profile</title>
</svelte:head>

<h1 class="title">Profile</h1>

{#if $CurrentUser != null}
	<div class="container" style="max-width:800px;">
		<form
			action="{BASE_URL}/account/update"
			method="POST"
			class="box"
			use:enhance={{
				result: (res, form) => {}
			}}
		>
			<input name="id" type="hidden" value={$CurrentUser.id} />

			<div class="field">
				<label class="label">Username</label>
				<div class="control">
					<input
						name="name"
						class="input"
						type="text"
						placeholder="Enter Username"
						value={$CurrentUser.name}
						disabled
						required
					/>
				</div>
			</div>

			<div class="field">
				<label class="label">Token</label>
				<div class="field has-addons">
					<div class="control is-expanded">
						<input name="token" class="input" type="password" placeholder="Token" />
					</div>
					<div class="control">
						<button class="button is-warning"> Refresh </button>
					</div>
				</div>
			</div>

			<div class="field">
				<div class="control">
					<button type="submit" class="button is-primary">Update</button>
				</div>
			</div>
		</form>

		<form
			action="{BASE_URL}/account/update"
			method="POST"
			class="box"
			use:enhance={{
				result: (res, form) => {},
				error: async (res, form) => {
					let json = await res.json();
					errors = [...json];
				}
			}}
		>
			<input name="id" type="hidden" value={$CurrentUser.id} />

			{#each errors as error}
				<div class="notification is-danger">{error}</div>
			{/each}

			<div class="field">
				<label class="label">Current Password</label>
				<div class="control">
					<input
						name="pass_old"
						class="input"
						type="password"
						placeholder="Old Password"
					/>
				</div>
			</div>

			<div class="field">
				<label class="label">New Password</label>
				<div class="control">
					<input name="pass" class="input" type="password" placeholder="Enter Password" />
				</div>
			</div>

			<div class="field">
				<label class="label">Confirm Password</label>
				<div class="control">
					<input class="input" type="password" placeholder="Confirm Password" />
				</div>
			</div>

			<div class="field">
				<div class="control">
					<button type="submit" class="button is-primary">Update</button>
				</div>
			</div>
		</form>
	</div>
{/if}
