<script context="module" lang="ts">
	import type { Load } from '@sveltejs/kit';
	import { prerendering } from '$app/env';

	export const load: Load = async ({ fetch }) => {
		if (prerendering) return {};
		const res = await fetch(`${BASE_URL}/api/tab/stats/header`);
		const json: BlogView[] = await res.json();

		if (res.ok) {
			return {
				props: {
					blogViews: json
				}
			};
		}

		return { status: res.status };
	};
</script>

<script lang="ts">
	import { BASE_URL } from '$lib/util';
	import type { BlogView } from '$lib/api';

	export let blogViews: BlogView[] = [];
</script>

