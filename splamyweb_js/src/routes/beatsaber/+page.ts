import type { PageLoad } from './$types';
import { building } from "$app/environment";
import { error } from '@sveltejs/kit';
import type { RamsesSystemStats } from '$lib/api';
import { BASE_URL } from '$lib/util';

export const load: PageLoad = async ({ fetch }) => {
	if (building) return {};
	const res = await fetch(`${BASE_URL}/api/ramses/system/display`);
	const json: RamsesSystemStats = await res.json();

	if (res.ok) {
		return json;
	}

	throw error(res.status, "Load Error XXX TODO");
}
