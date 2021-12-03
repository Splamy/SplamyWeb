<script lang="ts">
	import moment from 'moment';
	import { Chart, registerables } from 'chart.js';
	import 'chartjs-adapter-moment';
	import { onMount } from 'svelte';
	import { BASE_URL } from '$lib/util';
	import { prerendering } from '$app/env';

	type GraphData = GraphDay[];
	interface GraphDay {
		date: string;
		runningInstances: number;
		runningBots: number;
		playbackTime: TimeSpan;
	}
	interface TimeSpan {
		ticks: number;
		days: number;
		hours: number;
		milliseconds: number;
		minutes: number;
		seconds: number;
		totalDays: number;
		totalHours: number;
		totalMilliseconds: number;
		totalMinutes: number;
		totalSeconds: number;
	}

	// TODO sveltify !!!

	async function init() {
		if (prerendering) return;

		Chart.register(...registerables);

		const ctx = (document.getElementById('tsab_graph') as HTMLCanvasElement).getContext('2d');
		const response = await fetch(`${BASE_URL}/api/tab/stats/graph`);
		const data = (await response.json()) as GraphData;

		function cjsd(date) {
			return moment(date).valueOf();
		}

		const mappedRunningInstances = data.map((x) => {
			return { x: cjsd(x.date), y: x.runningInstances };
		});
		const mappedRunningBots = data.map((x) => {
			return { x: cjsd(x.date), y: x.runningBots };
		});
		const mappedPlayTime = data.map((x) => {
			return { x: cjsd(x.date), y: Number(x.playbackTime.totalDays.toFixed(1)) };
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
	<canvas id="tsab_graph" width="400" height="400" />
</section>
