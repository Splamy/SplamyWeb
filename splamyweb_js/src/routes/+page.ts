import { error } from '@sveltejs/kit';
import { BASE_URL } from '$lib/util';
import type { BlogListQuery } from '$lib/api';
import type { PageLoad } from './$types';
import { building } from "$app/environment";

export const load: PageLoad = async ({ fetch }) => {
	if (building) return {};
	const res = await fetch(`${BASE_URL}/api/content/home`);
	const json: BlogListQuery = await res.json();

	if (res.ok) {
		return {
			query: json
		};
	}

	throw error(res.status, "Failed to load home content");
};
