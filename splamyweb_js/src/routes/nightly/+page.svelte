<script lang="ts">
	import swal from 'sweetalert';
	import type { ProjectInfo } from '$lib/api';
	import Icon from '$lib/Icon.svelte';
	import ShortDate from '$lib/ShortDate.svelte';
	import { mdiClose } from '@mdi/js';
	import { BASE_URL } from '$lib/util';
	import type { PageData } from './$types';

	export let data: PageData;

	async function askDeleteNightly(project: string, branch: string) {
		const answer = await swal(`Delete ${project}/${branch} ?`, {
			dangerMode: true,
			buttons: ['Cancel', 'Delete']
		});
		if (answer === true) {
			try {
				const response = await fetch(
					`${BASE_URL}/api/nightly/projects/${project}/${branch}`,
					{
						method: 'DELETE',
						credentials: 'include'
					}
				);
				if (!response.ok) {
					throw response;
				}

				swal.close();
			} catch {
				await swal('Failed to delete branch', {
					icon: 'error'
				});
			}
		}
	}
</script>

<svelte:head>
	<title>Nightly</title>
</svelte:head>

<h1 class="title">Nightly Builds</h1>

<!-- {@debug projects} -->
{#each data.projects as project}
	<div class="box">
		<h2 id={project.project} class="is-size-4">{project.projectName ?? '<unnamed>'}:</h2>
		{#if project.notification}
			{@html project.notification}
		{/if}
		<table class="table" style="width:100%;">
			<tr>
				<th>Branch</th>
				<th>Version</th>
				<th>Commit</th>
				<th>Upload Date</th>
				<th />
				{#if project.extended}
					<th>Count</th>
					<th>Options</th>
				{/if}
			</tr>
			{#each project.builds as entry}
				<tr>
					<td>{entry.branch}</td>
					<td>{entry.version}</td>
					<td style="text-transform: uppercase;">
						{#if project.commitUrl != null && project.commitUrl.includes('{0}')}
							<a href={project.commitUrl.replace(/\{0\}/, entry.commit)}
								>{entry.commit.substring(0, 8)}</a
							>
						{:else}
							{entry.commit.substring(0, 8)}
						{/if}
					</td>
					<td><ShortDate date={entry.uploadTime} /></td>
					<td>
						{#if entry.active !== false}
							<a
								rel="external"
								href="{BASE_URL}/api/nightly/projects/{project.project}/{entry.branch}/download"
								download={entry.fileName}
								class="button is-primary is-rounded is-small"
							>
								Download
							</a>
						{/if}
					</td>
					{#if project.extended}
						<td>{entry.downloadCount}</td>
						<td>
							{#if entry.active !== false}
								<button
									title="Delete branch"
									class="button is-primary is-tool is-small compact"
									on:click={() => askDeleteNightly(project.project, entry.branch)}
								>
									<Icon path={mdiClose} />
								</button>
							{/if}
						</td>
					{/if}
				</tr>
			{/each}
		</table>
	</div>
{/each}

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/elements/title';
	@import 'bulma/sass/elements/table';
	@import 'bulma/sass/elements/button';
	@import 'bulma/sass/elements/box';
</style>
