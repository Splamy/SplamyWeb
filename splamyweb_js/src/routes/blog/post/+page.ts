import { BASE_URL } from '$lib/util';
import type { BlogItemQuery } from '$lib/api';
import type { PageLoad } from './$types';
import { building } from "$app/environment";
import { error } from '@sveltejs/kit';

export const load: PageLoad = async ({ fetch, url }) => {
	if (building) return {};

	const post = url.searchParams.get('i');
	if (post) {
		const res = await fetch(`${BASE_URL}/api/content/post/${post}`, {
			credentials: 'include'
		});
		const json: BlogItemQuery = await res.json();

		if (res.ok) {
			return {
				query: json,
				postId: post
			};
		}

		throw error(res.status, "Load Error XXX TODO");
	}
	return {};
};
