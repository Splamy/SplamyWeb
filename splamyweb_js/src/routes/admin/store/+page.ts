import type { PageLoad } from './$types';
import { error } from '@sveltejs/kit';
import type { KeyValue } from '$lib/api';
import { browser } from '$app/environment';
import { BASE_URL } from '$lib/util';

export const load: PageLoad = async ({ fetch }) => {
	if (!browser) {
		return {
			kvpList: []
		};
	}

	const res = await fetch(`${BASE_URL}/api/store/all`, {
		credentials: 'include'
	});
	const kvpList = (await res.json()) as KeyValue[];

	if (res.ok) {
		return {
			kvpList
		};
	}

	throw error(res.status, "Load Error XXX TODO");
};
