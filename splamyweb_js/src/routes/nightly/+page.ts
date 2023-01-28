import type { PageLoad } from './$types';
import { building } from "$app/environment";
import { BASE_URL } from '$lib/util';
import { error } from '@sveltejs/kit';
import type { ProjectInfo } from '$lib/api';

export const load: PageLoad = async ({ fetch }) => {
	if (building) {
		return {
			projects: []
		};
	}
	const res = await fetch(`${BASE_URL}/api/nightly/projects`, {
		credentials: 'include'
	});
	const projects = (await res.json()) as ProjectInfo[];

	if (res.ok) {
		return {
			projects
		};
	}

	throw error(res.status, "Load Error XXX TODO");
};
