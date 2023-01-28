import { BASE_URL } from '$lib/util';
import type { BlogPostUpdate } from '$lib/api';
import type { PageLoad } from './$types';
import { building } from "$app/environment";
import { error } from '@sveltejs/kit';

export const load: PageLoad = async ({ fetch, url }) => {
	if (building) {
		return {
			postId: undefined,
			postEdit: {
				contentRaw: '',
				visible: true,
				tags: []
			}
		};
	}

	const post = url.searchParams.get('post');
	if (post) {
		const res = await fetch(`${BASE_URL}/api/content/post/${post}/raw`, {
			credentials: 'include'
		});
		const json: BlogPostUpdate = await res.json();

		if (res.ok) {
			return {
				postEdit: json,
				postId: post
			};
		}

		throw error(res.status, "Load Error XXX TODO");
	}
	return {};
};
