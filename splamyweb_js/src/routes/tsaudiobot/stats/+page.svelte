<script lang="ts">
	import 'chartjs-adapter-moment';
	import Chart from 'chart.js/auto';
	import moment from 'moment';
	import { onMount } from 'svelte';
	import { BASE_URL } from '$lib/util';

	type GraphData = GraphDay[];
	interface GraphDay {
		date: string;
		runningInstances: number;
		runningBots: number;
		playbackTime: string;
	}

	let canvas: HTMLCanvasElement;

	async function init() {
		const ctx = canvas.getContext('2d');
		const response = await fetch(`${BASE_URL}/api/tab/stats/graph`);
		const data = (await response.json()) as GraphData;

		function cjsd(date: string): number {
			return moment(date).valueOf();
		}

		const mappedRunningInstances = data.map((x) => {
			return { x: cjsd(x.date), y: x.runningInstances };
		});
		const mappedRunningBots = data.map((x) => {
			return { x: cjsd(x.date), y: x.runningBots };
		});
		const mappedPlayTime = data.map((x) => {
			return { x: cjsd(x.date), y: moment.duration(x.playbackTime).asDays() };
		});

		const cRed = 'rgb(255, 99, 132)';
		const cYellow = 'rgb(255, 205, 86)';
		const cGreen = 'rgb(75, 192, 192)';

		const chart = new Chart(ctx, {
			type: 'line',
			data: {
				datasets: [
					{
						label: 'Instances',
						backgroundColor: cRed,
						borderColor: cRed,
						data: mappedRunningInstances,
						fill: false,
						yAxisID: 'y-axis-count'
					},
					{
						label: 'Bots',
						backgroundColor: cYellow,
						borderColor: cYellow,
						data: mappedRunningBots,
						fill: false,
						yAxisID: 'y-axis-count'
					},
					{
						label: 'Playtime',
						type: 'bar',
						backgroundColor: cGreen,
						borderColor: cGreen,
						data: mappedPlayTime,
						yAxisID: 'y-axis-time'
					}
				]
			},
			options: {
				animation: false,
				responsive: true,
				maintainAspectRatio: false,
				aspectRatio: 2,
				plugins: {
					tooltip: {
						mode: 'index',
						intersect: false
					},
					title: {
						display: false,
						text: 'TS3AudioBot Stats'
					}
				},
				hover: {
					mode: 'index',
					intersect: false
				},
				scales: {
					'y-axis-count': {
						display: true,
						title: {
							display: true,
							text: 'Count'
						}
					},
					'y-axis-time': {
						display: true,
						title: {
							display: true,
							text: 'Time (Days)'
						},
						grid: {
							drawOnChartArea: false // only want the grid lines for one axis to show up
						},
						position: 'right'
					},
					xAxis: {
						type: 'timeseries',
						offset: true,
						ticks: {
							major: {
								enabled: true
							},
							font: {
								weight: 'bold'
							},
							source: 'data',
							autoSkip: true,
							autoSkipPadding: 75,
							maxRotation: 0,
							sampleSize: 100
						}
					}
				}
			}
		});

		(window as any).myLine = chart;
	}
	onMount(() => {
		init();
	});
</script>

<svelte:head>
	<title>TSAudioBot Stats</title>
</svelte:head>

<h1 class="title">TSAudioBot Stats</h1>

<section class="section">
	<canvas bind:this={canvas} width="400" height="400" />
</section>

<style lang="scss">
	@import 'bulma/sass/layout/section';
</style>
