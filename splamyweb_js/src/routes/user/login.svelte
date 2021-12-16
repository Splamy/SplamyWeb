<script lang="ts">
	import { goto } from '$app/navigation';
	import { applyLoginResult } from '$lib/user';
	import { BASE_URL, enhance } from '$lib/util';

	let errors: string[] = [];
</script>

<svelte:head>
	<title>User</title>
</svelte:head>

<h1 class="title">Login</h1>

<div class="readblock">
	{#each errors as error}
		<div class="notification is-danger">{error}</div>
	{/each}

	<form
		action="{BASE_URL}/account/login"
		method="POST"
		class="box"
		use:enhance={{
			result: async (res, form) => {
				applyLoginResult(await res.json());

				let queryParams = new URLSearchParams(window.location.search);
				let returnUrl = queryParams.get('return');
				form.reset();
				goto(returnUrl ?? `/`);
			},
			error: async (res, form) => {
				let json = await res.json();
				errors = [...json];
			}
		}}
	>
		<div class="field">
			<label class="label" for="username">Username</label>
			<div class="control">
				<input
					id="username"
					name="name"
					class="input"
					type="text"
					placeholder="Username"
					required
				/>
			</div>
		</div>

		<div class="field">
			<label class="label" for="password">Password</label>
			<div class="control">
				<input
					id="password"
					name="pass"
					class="input"
					type="password"
					placeholder="Password"
				/>
			</div>
		</div>

		<div class="field">
			<div class="control">
				<button type="submit" class="button is-primary">Login</button>
			</div>
		</div>
	</form>
</div>

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/form/shared';
	@import 'bulma/sass/form/tools';
	@import 'bulma/sass/form/input-textarea';
	@import 'bulma/sass/elements/notification';
	@import 'bulma/sass/elements/box';
	@import 'bulma/sass/elements/button';
</style>
