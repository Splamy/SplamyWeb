import type { PageLoad } from './$types';
import { building } from "$app/environment";
import { error } from '@sveltejs/kit';
import type { TabStatsHeader } from '$lib/api';
import { BASE_URL } from '$lib/util';

export const load: PageLoad = async ({ fetch }) => {
	if (building) return {};
	const res = await fetch(`${BASE_URL}/api/tab/stats/header`);
	const json: TabStatsHeader = await res.json();

	if (res.ok) {
		return {
			stat_downloads: json.downloads,
			stat_instances: json.runningInstances,
			stat_bots: json.runningBots,
			stat_playtime: json.playbackTime
		};
	}

	throw error(res.status, "Load Error XXX TODO");
};
