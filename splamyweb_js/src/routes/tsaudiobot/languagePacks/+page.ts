import type { PageLoad } from './$types';
import { building } from "$app/environment";
import type { LangInfo } from '$lib/api';
import { BASE_URL } from '$lib/util';
import { error } from '@sveltejs/kit';

export const load: PageLoad = async ({ fetch }) => {
	if (building) {
		return {
			langs: []
		};
	}

	const res = await fetch(`${BASE_URL}/api/language/project/ts3ab/languages`, {
		credentials: 'include'
	});
	const langs = (await res.json()) as LangInfo[];

	if (res.ok) {
		return {
			langs
		};
	}

	throw error(res.status, "Load Error XXX TODO");
};
