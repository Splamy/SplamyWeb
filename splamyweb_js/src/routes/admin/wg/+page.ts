import type { PageLoad } from './$types';
import { error } from '@sveltejs/kit';
import { browser } from '$app/environment';
import { BASE_URL } from '$lib/util';
import type { WgPeer } from './defs';

export const load: PageLoad = async ({ fetch }) => {
	if (!browser) {
		return {
			peers: []
		};
	}

	const res = await fetch(`${BASE_URL}/api/wireguard/peers`, {
		credentials: 'include'
	});
	const peers = (await res.json()) as WgPeer[];

	if (res.ok) {
		return {
			peers
		};
	}

	throw error(res.status, "Load Error XXX TODO");
};
