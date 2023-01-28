import type { PageLoad } from './$types';
import { CurrentUser, User } from '$lib/user';
import { get } from 'svelte/store';
import { redirect } from '@sveltejs/kit';
import { building } from '$app/environment';

// see https://kit.svelte.dev/docs#loading
export const load: PageLoad = async () => {
	const user = building ? User.DUMMY : get(CurrentUser);
	if (user == null) {
		throw redirect(300, '/user/login');
	} else {
		return {};
	}
};
